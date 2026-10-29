using EmployeeManagement.API.Data;
using EmployeeManagement.API.DTOs;
using EmployeeManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.API.Services;

public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _db;
    public DepartmentService(ApplicationDbContext db) => _db = db;

    public Task<List<Department>> GetAllAsync() =>
        _db.Departments.AsNoTracking()
            .OrderBy(d => d.DepartmentName)
            .ToListAsync();

    public Task<Department?> GetByIdAsync(int id) =>
        _db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DepartmentId == id);

    public async Task<(Department? Department, string? Error, int StatusCode)> CreateAsync(CreateDepartmentDto dto)
    {
        var name = dto.DepartmentName.Trim();
        if (await _db.Departments.AnyAsync(d => d.DepartmentName == name))
            return (null, "Department name already exists.", StatusCodes.Status409Conflict);

        var department = new Department
        {
            DepartmentName = name,
            Description = dto.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Departments.Add(department);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(department.DepartmentId), null, StatusCodes.Status201Created);
    }

    public async Task<(Department? Department, string? Error, int StatusCode)> UpdateAsync(int id, UpdateDepartmentDto dto)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(d => d.DepartmentId == id);
        if (department is null)
            return (null, "Department not found.", StatusCodes.Status404NotFound);

        var name = dto.DepartmentName.Trim();
        if (await _db.Departments.AnyAsync(d => d.DepartmentId != id && d.DepartmentName == name))
            return (null, "Department name already exists.", StatusCodes.Status409Conflict);

        department.DepartmentName = name;
        department.Description = dto.Description?.Trim();
        department.IsActive = dto.IsActive;
        department.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(id), null, StatusCodes.Status200OK);
    }

    public async Task<(bool Success, string? Error, int StatusCode)> DeleteAsync(int id)
    {
        var department = await _db.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.DepartmentId == id);

        if (department is null)
            return (false, "Department not found.", StatusCodes.Status404NotFound);

        if (department.Employees.Count > 0)
            return (false, "Department has employees. Move them to another department before deleting.", StatusCodes.Status409Conflict);

        _db.Departments.Remove(department);
        await _db.SaveChangesAsync();
        return (true, null, StatusCodes.Status200OK);
    }
}
