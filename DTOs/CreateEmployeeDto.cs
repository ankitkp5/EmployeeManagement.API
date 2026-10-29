using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.API.DTOs;

public class CreateEmployeeDto
{
    [Required, StringLength(30)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(25)]
    public string? Phone { get; set; }

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [Required, StringLength(100)]
    public string Designation { get; set; } = string.Empty;

    [Required]
    public DateTime JoiningDate { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal Salary { get; set; }

    [Required]
    [RegularExpression("Active|Inactive|Resigned|Terminated")]
    public string EmploymentStatus { get; set; } = "Active";
}
