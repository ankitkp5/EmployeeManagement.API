using EmployeeManagement.API.DTOs;
using EmployeeManagement.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _service;
    public DepartmentsController(IDepartmentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var department = await _service.GetByIdAsync(id);
        return department is null ? NotFound(new { message = "Department not found." }) : Ok(department);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (result.Error is not null)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return CreatedAtAction(nameof(GetById), new { id = result.Department!.DepartmentId }, result.Department);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result.Error is not null)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Department);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (result.Error is not null)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return NoContent();
    }
}
