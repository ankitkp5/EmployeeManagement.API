using EmployeeManagement.API.Data;
using EmployeeManagement.API.DTOs;
using EmployeeManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.API.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _db;

    public EmployeeService(ApplicationDbContext db) => _db = db;

    public async Task<List<Employee>> GetAllAsync(string? search, int? departmentId, string? status)
    {
        var query = _db.Employees.Include(e => e.Department).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                e.EmployeeCode.Contains(term) ||
                e.FirstName.Contains(term) ||
                e.LastName.Contains(term) ||
                e.Email.Contains(term));
        }

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.EmploymentStatus == status);

        return await query.OrderByDescending(e => e.EmployeeId).ToListAsync();
    }

    public Task<Employee?> GetByIdAsync(int id) =>
        _db.Employees.Include(e => e.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == id);

    public async Task<(Employee? Employee, string? Error, int StatusCode)> CreateAsync(CreateEmployeeDto dto)
    {
        var departmentExists = await _db.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId);
        if (!departmentExists)
            return (null, "DepartmentId does not exist.", StatusCodes.Status400BadRequest);

        if (await _db.Employees.AnyAsync(e => e.Email == dto.Email))
            return (null, "Email already exists.", StatusCodes.Status409Conflict);

        if (await _db.Employees.AnyAsync(e => e.EmployeeCode == dto.EmployeeCode))
            return (null, "EmployeeCode already exists.", StatusCodes.Status409Conflict);

        var employee = new Employee
        {
            EmployeeCode = dto.EmployeeCode.Trim(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone?.Trim(),
            DepartmentId = dto.DepartmentId,
            Designation = dto.Designation.Trim(),
            JoiningDate = dto.JoiningDate.Date,
            Salary = dto.Salary,
            EmploymentStatus = dto.EmploymentStatus,
            CreatedAt = DateTime.UtcNow
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(employee.EmployeeId), null, StatusCodes.Status201Created);
    }

    public async Task<(Employee? Employee, string? Error, int StatusCode)> UpdateAsync(int id, UpdateEmployeeDto dto)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id);
        if (employee is null)
            return (null, "Employee not found.", StatusCodes.Status404NotFound);

        if (!await _db.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId))
            return (null, "DepartmentId does not exist.", StatusCodes.Status400BadRequest);

        if (await _db.Employees.AnyAsync(e => e.EmployeeId != id && e.Email == dto.Email))
            return (null, "Email already exists.", StatusCodes.Status409Conflict);

        if (await _db.Employees.AnyAsync(e => e.EmployeeId != id && e.EmployeeCode == dto.EmployeeCode))
            return (null, "EmployeeCode already exists.", StatusCodes.Status409Conflict);

        employee.EmployeeCode = dto.EmployeeCode.Trim();
        employee.FirstName = dto.FirstName.Trim();
        employee.LastName = dto.LastName.Trim();
        employee.Email = dto.Email.Trim();
        employee.Phone = dto.Phone?.Trim();
        employee.DepartmentId = dto.DepartmentId;
        employee.Designation = dto.Designation.Trim();
        employee.JoiningDate = dto.JoiningDate.Date;
        employee.Salary = dto.Salary;
        employee.EmploymentStatus = dto.EmploymentStatus;
        employee.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (await GetByIdAsync(id), null, StatusCodes.Status200OK);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id);
        if (employee is null) return false;

        // The database restricts deletion if attendance/salary/performance records exist.
        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();
        return true;
    }
}
