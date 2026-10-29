using EmployeeManagement.API.DTOs;
using EmployeeManagement.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);

        if (existingUser != null)
        {
            return Conflict(new
            {
                message = "An account with this email already exists."
            });
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName.Trim(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Registration failed.",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        return StatusCode(201, new
        {
            message = "Registration successful."
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());

        if (user == null || !user.IsActive)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var jwtKey = _configuration["Jwt:Key"]!;
        var jwtIssuer = _configuration["Jwt:Issuer"]!;
        var jwtAudience = _configuration["Jwt:Audience"]!;

        var expiryMinutes = int.TryParse(
            _configuration["Jwt:ExpiryMinutes"],
            out var minutes) && minutes > 0
                ? minutes
                : 60;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var roles = await _userManager.GetRolesAsync(user);

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return Ok(new
        {
            message = "Login successful.",
            token = tokenString,
            expiresInMinutes = expiryMinutes,
            user = new
            {
                user.Id,
                user.FullName,
                user.Email
            }
        });
    }
}