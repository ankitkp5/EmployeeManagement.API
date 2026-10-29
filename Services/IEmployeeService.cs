using EmployeeManagement.API.DTOs;
using EmployeeManagement.API.Models;

namespace EmployeeManagement.API.Services;

public interface IEmployeeService
{
    Task<List<Employee>> GetAllAsync(string? search, int? departmentId, string? status);
    Task<Employee?> GetByIdAsync(int id);
    Task<(Employee? Employee, string? Error, int StatusCode)> CreateAsync(CreateEmployeeDto dto);
    Task<(Employee? Employee, string? Error, int StatusCode)> UpdateAsync(int id, UpdateEmployeeDto dto);
    Task<bool> DeleteAsync(int id);
}
