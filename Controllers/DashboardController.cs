using EmployeeManagement.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/Dashboard/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var today = DateTime.Today;

        var totalEmployees = await _db.Employees.CountAsync();

        var activeEmployees = await _db.Employees
            .CountAsync(e => e.EmploymentStatus == "Active");

        var inactiveEmployees = await _db.Employees
            .CountAsync(e => e.EmploymentStatus == "Inactive");

        var resignedEmployees = await _db.Employees
            .CountAsync(e => e.EmploymentStatus == "Resigned");

        var terminatedEmployees = await _db.Employees
            .CountAsync(e => e.EmploymentStatus == "Terminated");

        var totalDepartments = await _db.Departments.CountAsync();

        var totalSalary = await _db.Employees
            .Select(e => (decimal?)e.Salary)
            .SumAsync() ?? 0m;

        var todayAttendance = await _db.Attendances
            .Where(a => a.AttendanceDate == today)
            .GroupBy(a => a.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        todayAttendance.TryGetValue("Present", out var present);
        todayAttendance.TryGetValue("Absent", out var absent);
        todayAttendance.TryGetValue("Half Day", out var halfDay);
        todayAttendance.TryGetValue("Leave", out var leave);

        return Ok(new
        {
            Employees = new
            {
                Total = totalEmployees,
                Active = activeEmployees,
                Inactive = inactiveEmployees,
                Resigned = resignedEmployees,
                Terminated = terminatedEmployees
            },

            Departments = new
            {
                Total = totalDepartments
            },

            Salary = new
            {
                TotalMonthlySalary = totalSalary
            },

            Attendance = new
            {
                Date = today.ToString("yyyy-MM-dd"),
                Present = present,
                Absent = absent,
                HalfDay = halfDay,
                Leave = leave,
                TotalMarked = present + absent + halfDay + leave
            }
        });
    }

    // GET: api/Dashboard/department-distribution
    [HttpGet("department-distribution")]
    public async Task<IActionResult> GetDepartmentDistribution()
    {
        var result = await _db.Departments
            .AsNoTracking()
            .Select(d => new
            {
                DepartmentId = d.DepartmentId,
                DepartmentName = d.DepartmentName,

                EmployeeCount = _db.Employees
                    .Count(e => e.DepartmentId == d.DepartmentId)
            })
            .OrderByDescending(d => d.EmployeeCount)
            .ToListAsync();

        return Ok(result);
    }

    // GET: api/Dashboard/hiring-trend
    [HttpGet("hiring-trend")]
    public async Task<IActionResult> GetHiringTrend()
    {
        var currentMonth = new DateTime(
            DateTime.Today.Year,
            DateTime.Today.Month,
            1);

        var startMonth = currentMonth.AddMonths(-5);

        var employees = await _db.Employees
            .AsNoTracking()
            .Where(e =>
                e.JoiningDate >= startMonth &&
                e.JoiningDate < currentMonth.AddMonths(1))
            .Select(e => e.JoiningDate)
            .ToListAsync();

        var monthlyData = employees
            .GroupBy(d => new { d.Year, d.Month })
            .ToDictionary(g => (g.Key.Year, g.Key.Month), g => g.Count());

        var result = Enumerable.Range(0, 6)
            .Select(i =>
            {
                var month = startMonth.AddMonths(i);

                monthlyData.TryGetValue(
                    (month.Year, month.Month),
                    out var count);

                return new
                {
                    Year = month.Year,
                    Month = month.Month,
                    MonthName = month.ToString("MMM yyyy"),
                    NewHires = count
                };
            })
            .ToList();

        return Ok(result);
    }
}