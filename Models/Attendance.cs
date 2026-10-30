using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeManagement.API.Models;

public class Attendance
{
    public int AttendanceId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime AttendanceDate { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Present";

    public TimeSpan? CheckIn { get; set; }

    public TimeSpan? CheckOut { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }
}