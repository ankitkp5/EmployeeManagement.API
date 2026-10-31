using EmployeeManagement.API.DTOs;
using EmployeeManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _service;

    public EmployeesController(IEmployeeService service)
    {
        _service = service;
    }

    // GET: api/Employees
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? departmentId,
        [FromQuery] string? status)
    {
        var employees = await _service.GetAllAsync(
            search, departmentId, status);

        return Ok(employees);
    }

    // GET: api/Employees/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var employee = await _service.GetByIdAsync(id);

        return employee is null
            ? NotFound(new { message = "Employee not found." })
            : Ok(employee);
    }

    // POST: api/Employees
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployeeDto dto)
    {
        var result = await _service.CreateAsync(dto);

        if (result.Error is not null)
        {
            return StatusCode(
                result.StatusCode,
                new { message = result.Error });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Employee!.EmployeeId },
            result.Employee);
    }

    // POST: api/Employees/bulk
    // Creates multiple employees in one request.
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreate(
        [FromBody] List<CreateEmployeeDto> employees)
    {
        if (employees is null || employees.Count == 0)
        {
            return BadRequest(new
            {
                message = "At least one employee is required."
            });
        }

        var result = await _service.BulkCreateAsync(employees);

        if (result.Error is not null)
        {
            return StatusCode(
                result.StatusCode,
                new { message = result.Error });
        }

        return Ok(new
        {
            message = $"{result.Employees.Count} employees created successfully.",
            employees = result.Employees
        });
    }

    // PUT: api/Employees/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateEmployeeDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);

        if (result.Error is not null)
        {
            return StatusCode(
                result.StatusCode,
                new { message = result.Error });
        }

        return Ok(result.Employee);
    }

    // DELETE: api/Employees/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);

            return deleted
                ? NoContent()
                : NotFound(new { message = "Employee not found." });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Employee has related records and cannot be deleted. Mark the employee Inactive instead."
            });
        }
    }

    // DELETE: api/Employees/bulk
    // Deletes multiple employees by their IDs.
    [HttpDelete("bulk")]
    public async Task<IActionResult> BulkDelete(
        [FromBody] List<int> employeeIds)
    {
        if (employeeIds is null || employeeIds.Count == 0)
        {
            return BadRequest(new
            {
                message = "Select at least one employee to delete."
            });
        }

        if (employeeIds.Any(id => id <= 0))
        {
            return BadRequest(new
            {
                message = "All employee IDs must be positive numbers."
            });
        }

        try
        {
            var result = await _service.BulkDeleteAsync(employeeIds);

            if (result.Error is not null)
            {
                return StatusCode(
                    result.StatusCode,
                    new { message = result.Error });
            }

            return Ok(new
            {
                message = $"{result.DeletedCount} employees deleted successfully.",
                deletedCount = result.DeletedCount
            });
        }

        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "One or more employees have related records and could not be deleted."
            });
        }
    }
}