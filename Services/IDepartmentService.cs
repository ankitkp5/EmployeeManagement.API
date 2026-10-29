using EmployeeManagement.API.DTOs;
using EmployeeManagement.API.Models;

namespace EmployeeManagement.API.Services;

public interface IDepartmentService
{
    Task<List<Department>> GetAllAsync();
    Task<Department?> GetByIdAsync(int id);
    Task<(Department? Department, string? Error, int StatusCode)> CreateAsync(CreateDepartmentDto dto);
    Task<(Department? Department, string? Error, int StatusCode)> UpdateAsync(int id, UpdateDepartmentDto dto);
    Task<(bool Success, string? Error, int StatusCode)> DeleteAsync(int id);
}
