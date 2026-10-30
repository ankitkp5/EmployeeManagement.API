using EmployeeManagement.API.Data;
using EmployeeManagement.API.DTOs;
using EmployeeManagement.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AttendanceController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/Attendance?date=2026-10-30
    // GET: api/Attendance?employeeId=1
    // GET: api/Attendance?from=2026-10-01&to=2026-10-30
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? employeeId,
        [FromQuery] DateTime? date,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        if (from.HasValue && to.HasValue &&
            from.Value.Date > to.Value.Date)
        {
            return BadRequest(new
            {
                message = "'from' date cannot be after 'to' date."
            });
        }

        var query = _db.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(
                a => a.EmployeeId == employeeId.Value);
        }

        if (date.HasValue)
        {
            var selectedDate = date.Value.Date;

            query = query.Where(a =>
                a.AttendanceDate >= selectedDate &&
                a.AttendanceDate < selectedDate.AddDays(1));
        }

        if (from.HasValue)
        {
            var startDate = from.Value.Date;
            query = query.Where(
                a => a.AttendanceDate >= startDate);
        }

        if (to.HasValue)
        {
            var endExclusive = to.Value.Date.AddDays(1);
            query = query.Where(
                a => a.AttendanceDate < endExclusive);
        }

        var records = await query
            .OrderByDescending(a => a.AttendanceDate)
            .ThenBy(a => a.EmployeeId)
            .Select(a => new
            {
                a.AttendanceId,
                a.EmployeeId,
                EmployeeCode = a.Employee!.EmployeeCode,
                EmployeeName =
                    a.Employee.FirstName + " " + a.Employee.LastName,
                a.AttendanceDate,
                a.Status,
                a.CheckIn,
                a.CheckOut,
                a.Remarks
            })
            .ToListAsync();

        return Ok(records);
    }

    // GET: api/Attendance/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var record = await _db.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.AttendanceId == id);

        if (record is null)
        {
            return NotFound(new
            {
                message = "Attendance record not found."
            });
        }

        return Ok(record);
    }

    // POST: api/Attendance
    [HttpPost]
    public async Task<IActionResult> Mark(
        [FromBody] MarkAttendanceDto dto)
    {
        var employeeExists = await _db.Employees
            .AnyAsync(e => e.EmployeeId == dto.EmployeeId);

        if (!employeeExists)
        {
            return BadRequest(new
            {
                message = "Employee does not exist."
            });
        }

        if (dto.CheckIn.HasValue &&
            dto.CheckOut.HasValue &&
            dto.CheckOut.Value < dto.CheckIn.Value)
        {
            return BadRequest(new
            {
                message = "Check-out cannot be earlier than check-in."
            });
        }

        var attendanceDate = dto.AttendanceDate.Date;

        var alreadyExists = await _db.Attendances.AnyAsync(a =>
            a.EmployeeId == dto.EmployeeId &&
            a.AttendanceDate == attendanceDate);

        if (alreadyExists)
        {
            return Conflict(new
            {
                message = "Attendance already exists for this employee and date. Use PUT to update it."
            });
        }

        var attendance = new Attendance
        {
            EmployeeId = dto.EmployeeId,
            AttendanceDate = attendanceDate,
            Status = dto.Status,
            CheckIn = dto.CheckIn,
            CheckOut = dto.CheckOut,
            Remarks = dto.Remarks?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Attendances.Add(attendance);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = attendance.AttendanceId },
            attendance);
    }

    // PUT: api/Attendance/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] MarkAttendanceDto dto)
    {
        var attendance = await _db.Attendances
            .FirstOrDefaultAsync(a => a.AttendanceId == id);

        if (attendance is null)
        {
            return NotFound(new
            {
                message = "Attendance record not found."
            });
        }

        var employeeExists = await _db.Employees
            .AnyAsync(e => e.EmployeeId == dto.EmployeeId);

        if (!employeeExists)
        {
            return BadRequest(new
            {
                message = "Employee does not exist."
            });
        }

        if (dto.CheckIn.HasValue &&
            dto.CheckOut.HasValue &&
            dto.CheckOut.Value < dto.CheckIn.Value)
        {
            return BadRequest(new
            {
                message = "Check-out cannot be earlier than check-in."
            });
        }

        var attendanceDate = dto.AttendanceDate.Date;

        var duplicateExists = await _db.Attendances.AnyAsync(a =>
            a.AttendanceId != id &&
            a.EmployeeId == dto.EmployeeId &&
            a.AttendanceDate == attendanceDate);

        if (duplicateExists)
        {
            return Conflict(new
            {
                message = "Attendance already exists for this employee and date."
            });
        }

        attendance.EmployeeId = dto.EmployeeId;
        attendance.AttendanceDate = attendanceDate;
        attendance.Status = dto.Status;
        attendance.CheckIn = dto.CheckIn;
        attendance.CheckOut = dto.CheckOut;
        attendance.Remarks = dto.Remarks?.Trim();

        await _db.SaveChangesAsync();

        return Ok(attendance);
    }

    // DELETE: api/Attendance/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var attendance = await _db.Attendances
            .FirstOrDefaultAsync(a => a.AttendanceId == id);

        if (attendance is null)
        {
            return NotFound(new
            {
                message = "Attendance record not found."
            });
        }

        _db.Attendances.Remove(attendance);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}