using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.API.DTOs;

public class UpdateDepartmentDto
{
    [Required, StringLength(100)]
    public string DepartmentName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
