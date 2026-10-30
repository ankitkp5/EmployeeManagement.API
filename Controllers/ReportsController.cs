using ClosedXML.Excel;
using EmployeeManagement.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EmployeeManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ReportsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // =====================================================
    // 1. EMPLOYEE REPORT - EXCEL
    // GET: api/Reports/employees/excel
    // =====================================================

    [HttpGet("employees/excel")]
    public async Task<IActionResult> ExportEmployeesToExcel()
    {
        var employees = await _db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .OrderBy(e => e.EmployeeId)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Employees");

        worksheet.Cell("A1").Value = "EMPLOYEE REPORT";
        worksheet.Range("A1:K1").Merge();

        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 16;
        worksheet.Cell("A1").Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        string[] headers =
        {
            "Employee ID",
            "Employee Code",
            "First Name",
            "Last Name",
            "Email",
            "Phone",
            "Department",
            "Designation",
            "Joining Date",
            "Salary",
            "Employment Status"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(3, i + 1).Value = headers[i];
        }

        var header = worksheet.Range("A3:K3");
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;

        int row = 4;

        foreach (var employee in employees)
        {
            worksheet.Cell(row, 1).Value = employee.EmployeeId;
            worksheet.Cell(row, 2).Value = employee.EmployeeCode;
            worksheet.Cell(row, 3).Value = employee.FirstName;
            worksheet.Cell(row, 4).Value = employee.LastName;
            worksheet.Cell(row, 5).Value = employee.Email;
            worksheet.Cell(row, 6).Value = employee.Phone ?? "";
            worksheet.Cell(row, 7).Value =
                employee.Department?.DepartmentName ?? "";
            worksheet.Cell(row, 8).Value = employee.Designation;

            worksheet.Cell(row, 9).Value = employee.JoiningDate;
            worksheet.Cell(row, 9).Style.DateFormat.Format = "dd-MMM-yyyy";

            worksheet.Cell(row, 10).Value = employee.Salary;
            worksheet.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";

            worksheet.Cell(row, 11).Value = employee.EmploymentStatus;

            row++;
        }

        worksheet.SheetView.FreezeRows(3);
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"EmployeeReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx"
        );
    }


    // =====================================================
    // 2. EMPLOYEE DIRECTORY - PDF
    // GET: api/Reports/employees/pdf
    // =====================================================

    [HttpGet("employees/pdf")]
    public async Task<IActionResult> ExportEmployeesToPdf()
    {
        var employees = await _db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .OrderBy(e => e.EmployeeId)
            .ToListAsync();

        QuestPDF.Settings.License = LicenseType.Community;

        var pdf = Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header()
                    .Column(column =>
                    {
                        column.Item()
                            .Text("EMPLOYEE DIRECTORY")
                            .FontSize(20)
                            .Bold()
                            .AlignCenter();

                        column.Item()
                            .PaddingTop(5)
                            .Text($"Generated on: {DateTime.Now:dd-MMM-yyyy HH:mm}")
                            .AlignRight();
                    });

                page.Content()
                    .PaddingVertical(15)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);
                            columns.ConstantColumn(65);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(65);
                            columns.ConstantColumn(60);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("ID");
                            header.Cell().Element(HeaderCell).Text("Code");
                            header.Cell().Element(HeaderCell).Text("First Name");
                            header.Cell().Element(HeaderCell).Text("Last Name");
                            header.Cell().Element(HeaderCell).Text("Email");
                            header.Cell().Element(HeaderCell).Text("Department");
                            header.Cell().Element(HeaderCell).Text("Designation");
                            header.Cell().Element(HeaderCell).Text("Joining Date");
                            header.Cell().Element(HeaderCell).Text("Status");
                        });

                        foreach (var employee in employees)
                        {
                            table.Cell().Element(BodyCell)
                                .Text(employee.EmployeeId.ToString());

                            table.Cell().Element(BodyCell)
                                .Text(employee.EmployeeCode);

                            table.Cell().Element(BodyCell)
                                .Text(employee.FirstName);

                            table.Cell().Element(BodyCell)
                                .Text(employee.LastName);

                            table.Cell().Element(BodyCell)
                                .Text(employee.Email);

                            table.Cell().Element(BodyCell)
                                .Text(employee.Department?.DepartmentName ?? "");

                            table.Cell().Element(BodyCell)
                                .Text(employee.Designation);

                            table.Cell().Element(BodyCell)
                                .Text(employee.JoiningDate.ToString("dd-MMM-yyyy"));

                            table.Cell().Element(BodyCell)
                                .Text(employee.EmploymentStatus);
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Employee Management System | Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
        });

        byte[] pdfBytes = pdf.GeneratePdf();

        return File(
            pdfBytes,
            "application/pdf",
            $"EmployeeDirectory_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf"
        );
    }


    // =====================================================
    // 3. SALARY REPORT - EXCEL
    // GET: api/Reports/salaries/excel
    // =====================================================

    [HttpGet("salaries/excel")]
    public async Task<IActionResult> ExportSalariesToExcel()
    {
        var employees = await _db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .OrderBy(e => e.DepartmentId)
            .ThenBy(e => e.FirstName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Salary Report");

        worksheet.Cell("A1").Value = "EMPLOYEE SALARY REPORT";
        worksheet.Range("A1:F1").Merge();

        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 16;
        worksheet.Cell("A1").Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        string[] headers =
        {
            "Employee Code",
            "Employee Name",
            "Department",
            "Designation",
            "Employment Status",
            "Salary"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(3, i + 1).Value = headers[i];
        }

        worksheet.Range("A3:F3").Style.Font.Bold = true;
        worksheet.Range("A3:F3").Style.Fill.BackgroundColor = XLColor.LightBlue;

        int row = 4;

        foreach (var employee in employees)
        {
            worksheet.Cell(row, 1).Value = employee.EmployeeCode;

            worksheet.Cell(row, 2).Value =
                $"{employee.FirstName} {employee.LastName}";

            worksheet.Cell(row, 3).Value =
                employee.Department?.DepartmentName ?? "";

            worksheet.Cell(row, 4).Value = employee.Designation;
            worksheet.Cell(row, 5).Value = employee.EmploymentStatus;
            worksheet.Cell(row, 6).Value = employee.Salary;

            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";

            row++;
        }

        worksheet.Cell(row, 5).Value = "TOTAL SALARY";
        worksheet.Cell(row, 5).Style.Font.Bold = true;

        if (row > 4)
        {
            worksheet.Cell(row, 6).FormulaA1 = $"SUM(F4:F{row - 1})";
        }
        else
        {
            worksheet.Cell(row, 6).Value = 0;
        }

        worksheet.Cell(row, 6).Style.Font.Bold = true;
        worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";

        worksheet.SheetView.FreezeRows(3);
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"SalaryReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx"
        );
    }


    // =====================================================
    // 4. ATTENDANCE REPORT - EXCEL
    // GET: api/Reports/attendance/excel?from=2026-10-01&to=2026-10-30
    // =====================================================

    [HttpGet("attendance/excel")]
    public async Task<IActionResult> ExportAttendanceToExcel(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        if (from.Date > to.Date)
        {
            return BadRequest(new
            {
                message = "'from' date cannot be later than 'to' date."
            });
        }

        var startDate = from.Date;
        var endDateExclusive = to.Date.AddDays(1);

        var attendanceRecords = await _db.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
            .Where(a =>
                a.AttendanceDate >= startDate &&
                a.AttendanceDate < endDateExclusive)
            .OrderBy(a => a.AttendanceDate)
            .ThenBy(a => a.EmployeeId)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Attendance");

        worksheet.Cell("A1").Value = "ATTENDANCE REPORT";
        worksheet.Range("A1:H1").Merge();

        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 16;
        worksheet.Cell("A1").Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        worksheet.Cell("A2").Value = "From";
        worksheet.Cell("B2").Value = startDate;
        worksheet.Cell("B2").Style.DateFormat.Format = "dd-MMM-yyyy";

        worksheet.Cell("C2").Value = "To";
        worksheet.Cell("D2").Value = to.Date;
        worksheet.Cell("D2").Style.DateFormat.Format = "dd-MMM-yyyy";

        string[] headers =
        {
            "Attendance ID",
            "Employee Code",
            "Employee Name",
            "Department",
            "Attendance Date",
            "Status",
            "Check In",
            "Check Out"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(4, i + 1).Value = headers[i];
        }

        worksheet.Range("A4:H4").Style.Font.Bold = true;
        worksheet.Range("A4:H4").Style.Fill.BackgroundColor = XLColor.LightBlue;

        int row = 5;

        foreach (var attendance in attendanceRecords)
        {
            var employee = attendance.Employee;

            worksheet.Cell(row, 1).Value = attendance.AttendanceId;
            worksheet.Cell(row, 2).Value = employee?.EmployeeCode ?? "";

            worksheet.Cell(row, 3).Value = employee == null
                ? ""
                : $"{employee.FirstName} {employee.LastName}";

            worksheet.Cell(row, 4).Value =
                employee?.Department?.DepartmentName ?? "";

            worksheet.Cell(row, 5).Value = attendance.AttendanceDate;
            worksheet.Cell(row, 5).Style.DateFormat.Format = "dd-MMM-yyyy";

            worksheet.Cell(row, 6).Value = attendance.Status;

            worksheet.Cell(row, 7).Value =
                attendance.CheckIn?.ToString(@"hh\:mm\:ss") ?? "";

            worksheet.Cell(row, 8).Value =
                attendance.CheckOut?.ToString(@"hh\:mm\:ss") ?? "";

            row++;
        }

        worksheet.SheetView.FreezeRows(4);
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"AttendanceReport_{startDate:yyyyMMdd}_{to.Date:yyyyMMdd}.xlsx"
        );
    }


    // =====================================================
    // PDF TABLE STYLES
    // =====================================================

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .Background(Colors.Grey.Lighten2)
            .Padding(4)
            .Border(0.5f)
            .BorderColor(Colors.Grey.Medium)
            .DefaultTextStyle(x => x.Bold());
    }

    private static IContainer BodyCell(IContainer container)
    {
        return container
            .Padding(4)
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2);
    }
}