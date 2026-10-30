using EmployeeManagement.API.Data;
using EmployeeManagement.API.DTOs;
using EmployeeManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.API.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _db;

    public EmployeeService(ApplicationDbContext db) => _db = db;


    // Add these methods inside your existing EmployeeService class.

    public async Task<(
        List<Employee> Employees,
        string? Error,
        int StatusCode)> BulkCreateAsync(
            List<CreateEmployeeDto> employees)
    {
        if (employees == null || employees.Count == 0)
        {
            return (
                new List<Employee>(),
                "At least one employee is required.",
                400);
        }

        // Normalize values for duplicate checking.
        var normalizedCodes = employees
            .Select(e => e.EmployeeCode.Trim().ToUpper())
            .ToList();

        var normalizedEmails = employees
            .Select(e => e.Email.Trim().ToUpper())
            .ToList();

        if (normalizedCodes.Any(string.IsNullOrWhiteSpace) ||
            normalizedEmails.Any(string.IsNullOrWhiteSpace))
        {
            return (
                new List<Employee>(),
                "Employee code and email are required.",
                400);
        }

        // Check duplicates within the submitted list.
        if (normalizedCodes.Count != normalizedCodes.Distinct().Count())
        {
            return (
                new List<Employee>(),
                "Duplicate employee codes found in the submitted list.",
                400);
        }

        if (normalizedEmails.Count != normalizedEmails.Distinct().Count())
        {
            return (
                new List<Employee>(),
                "Duplicate email addresses found in the submitted list.",
                400);
        }

        // Check existing employee codes and emails in the database.
        var existingCodes = await _db.Employees
            .Where(e => normalizedCodes.Contains(
                e.EmployeeCode.ToUpper()))
            .Select(e => e.EmployeeCode)
            .ToListAsync();

        if (existingCodes.Count > 0)
        {
            return (
                new List<Employee>(),
                $"Employee code already exists: {string.Join(", ", existingCodes)}",
                409);
        }

        var existingEmails = await _db.Employees
            .Where(e => normalizedEmails.Contains(
                e.Email.ToUpper()))
            .Select(e => e.Email)
            .ToListAsync();

        if (existingEmails.Count > 0)
        {
            return (
                new List<Employee>(),
                $"Email already exists: {string.Join(", ", existingEmails)}",
                409);
        }

        // Verify all department IDs.
        var departmentIds = employees
            .Select(e => e.DepartmentId)
            .Distinct()
            .ToList();

        var existingDepartmentIds = await _db.Departments
            .Where(d => departmentIds.Contains(d.DepartmentId))
            .Select(d => d.DepartmentId)
            .ToListAsync();

        var invalidDepartmentIds = departmentIds
            .Except(existingDepartmentIds)
            .ToList();

        if (invalidDepartmentIds.Count > 0)
        {
            return (
                new List<Employee>(),
                $"Invalid department IDs: {string.Join(", ", invalidDepartmentIds)}",
                400);
        }

        // Create Employee entities.
        var newEmployees = employees.Select(dto => new Employee
        {
            EmployeeCode = dto.EmployeeCode.Trim(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone?.Trim(),
            DepartmentId = dto.DepartmentId,
            Designation = dto.Designation.Trim(),
            JoiningDate = dto.JoiningDate,
            Salary = dto.Salary,
            EmploymentStatus = dto.EmploymentStatus,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        try
        {
            await _db.Employees.AddRangeAsync(newEmployees);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (newEmployees, null, 200);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();

            return (
                new List<Employee>(),
                "Unable to save employees. Check the submitted data and database constraints.",
                409);
        }
    }

    public async Task<(
        int DeletedCount,
        string? Error,
        int StatusCode)> BulkDeleteAsync(
            List<int> employeeIds)
    {
        if (employeeIds == null || employeeIds.Count == 0)
        {
            return (0, "At least one employee ID is required.", 400);
        }

        if (employeeIds.Any(id => id <= 0))
        {
            return (0, "Employee IDs must be greater than zero.", 400);
        }

        // Remove duplicate IDs.
        var distinctIds = employeeIds.Distinct().ToList();

        var employees = await _db.Employees
            .Where(e => distinctIds.Contains(e.EmployeeId))
            .ToListAsync();

        // Require all requested employees to exist.
        var foundIds = employees
            .Select(e => e.EmployeeId)
            .ToHashSet();

        var missingIds = distinctIds
            .Where(id => !foundIds.Contains(id))
            .ToList();

        if (missingIds.Count > 0)
        {
            return (
                0,
                $"Employee IDs not found: {string.Join(", ", missingIds)}",
                404);
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        try
        {
            _db.Employees.RemoveRange(employees);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (employees.Count, null, 200);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();

            return (
                0,
                "Some employees have related records and cannot be deleted.",
                409);
        }
    }
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
