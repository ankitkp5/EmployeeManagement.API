using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.API.DTOs;

public class MarkAttendanceDto
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    public DateTime AttendanceDate { get; set; }

    [Required]
    [RegularExpression(
        "Present|Absent|Half Day|Leave",
        ErrorMessage = "Status must be Present, Absent, Half Day, or Leave.")]
    public string Status { get; set; } = "Present";

    public TimeSpan? CheckIn { get; set; }

    public TimeSpan? CheckOut { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }
}