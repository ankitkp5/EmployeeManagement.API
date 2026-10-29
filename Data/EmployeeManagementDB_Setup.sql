/*
    Employee Management System
    Database: SQL Server
    Run in SQL Server Management Studio (SSMS).

    NOTE:
    - This script creates the database if it does not already exist.
    - It creates application tables and reporting stored procedures.
    - It does NOT create ASP.NET Core Identity tables. Create those through
      EF Core migrations after configuring Identity in the API project.
    - Run the script against a SQL Server instance where you have permission
      to create databases.
*/

USE master;
GO

IF DB_ID(N'EmployeeManagementDB') IS NULL
BEGIN
    CREATE DATABASE EmployeeManagementDB;
END
GO

USE EmployeeManagementDB;
GO

/* =========================
   1. DEPARTMENTS
   ========================= */
IF OBJECT_ID(N'dbo.Departments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Departments
    (
        DepartmentId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Departments PRIMARY KEY,
        DepartmentName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Departments_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Departments_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT UQ_Departments_DepartmentName UNIQUE (DepartmentName)
    );
END
GO

/* =========================
   2. EMPLOYEES
   ========================= */
IF OBJECT_ID(N'dbo.Employees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employees
    (
        EmployeeId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Employees PRIMARY KEY,
        EmployeeCode NVARCHAR(30) NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(256) NOT NULL,
        Phone NVARCHAR(25) NULL,
        DepartmentId INT NOT NULL,
        Designation NVARCHAR(100) NOT NULL,
        JoiningDate DATE NOT NULL,
        Salary DECIMAL(18,2) NOT NULL,
        EmploymentStatus NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Employees_EmploymentStatus DEFAULT (N'Active'),
        CreatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_Employees_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT UQ_Employees_EmployeeCode UNIQUE (EmployeeCode),
        CONSTRAINT UQ_Employees_Email UNIQUE (Email),
        CONSTRAINT FK_Employees_Departments FOREIGN KEY (DepartmentId)
            REFERENCES dbo.Departments(DepartmentId),
        CONSTRAINT CK_Employees_Salary CHECK (Salary >= 0),
        CONSTRAINT CK_Employees_EmploymentStatus
            CHECK (EmploymentStatus IN (N'Active', N'Inactive', N'Resigned', N'Terminated'))
    );
END
GO

CREATE INDEX IX_Employees_DepartmentId ON dbo.Employees(DepartmentId);
CREATE INDEX IX_Employees_JoiningDate ON dbo.Employees(JoiningDate);
GO

/* =========================
   3. ATTENDANCE
   One record per employee per day.
   ========================= */
IF OBJECT_ID(N'dbo.Attendance', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Attendance
    (
        AttendanceId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Attendance PRIMARY KEY,
        EmployeeId INT NOT NULL,
        AttendanceDate DATE NOT NULL,
        CheckIn DATETIME2(0) NULL,
        CheckOut DATETIME2(0) NULL,
        AttendanceStatus NVARCHAR(20) NOT NULL,
        Remarks NVARCHAR(500) NULL,
        CreatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_Attendance_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT FK_Attendance_Employees FOREIGN KEY (EmployeeId)
            REFERENCES dbo.Employees(EmployeeId),
        CONSTRAINT UQ_Attendance_Employee_Date UNIQUE (EmployeeId, AttendanceDate),
        CONSTRAINT CK_Attendance_Status
            CHECK (AttendanceStatus IN (N'Present', N'Absent', N'Leave', N'HalfDay', N'Holiday')),
        CONSTRAINT CK_Attendance_CheckoutAfterCheckin
            CHECK (CheckIn IS NULL OR CheckOut IS NULL OR CheckOut >= CheckIn)
    );
END
GO

CREATE INDEX IX_Attendance_Date_Status
    ON dbo.Attendance(AttendanceDate, AttendanceStatus);
GO

/* =========================
   4. SALARY RECORDS
   One record per employee per month/year.
   ========================= */
IF OBJECT_ID(N'dbo.SalaryRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalaryRecords
    (
        SalaryRecordId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_SalaryRecords PRIMARY KEY,
        EmployeeId INT NOT NULL,
        SalaryMonth TINYINT NOT NULL,
        SalaryYear SMALLINT NOT NULL,
        BasicSalary DECIMAL(18,2) NOT NULL,
        Allowances DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalaryRecords_Allowances DEFAULT (0),
        Deductions DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalaryRecords_Deductions DEFAULT (0),
        NetSalary AS (BasicSalary + Allowances - Deductions) PERSISTED,
        PaymentStatus NVARCHAR(20) NOT NULL
            CONSTRAINT DF_SalaryRecords_PaymentStatus DEFAULT (N'Pending'),
        PaymentDate DATE NULL,
        Remarks NVARCHAR(500) NULL,
        CreatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_SalaryRecords_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT FK_SalaryRecords_Employees FOREIGN KEY (EmployeeId)
            REFERENCES dbo.Employees(EmployeeId),
        CONSTRAINT UQ_SalaryRecords_Employee_Month_Year
            UNIQUE (EmployeeId, SalaryMonth, SalaryYear),
        CONSTRAINT CK_SalaryRecords_Month CHECK (SalaryMonth BETWEEN 1 AND 12),
        CONSTRAINT CK_SalaryRecords_Year CHECK (SalaryYear BETWEEN 2000 AND 2100),
        CONSTRAINT CK_SalaryRecords_BasicSalary CHECK (BasicSalary >= 0),
        CONSTRAINT CK_SalaryRecords_Allowances CHECK (Allowances >= 0),
        CONSTRAINT CK_SalaryRecords_Deductions CHECK (Deductions >= 0),
        CONSTRAINT CK_SalaryRecords_NetSalary CHECK (BasicSalary + Allowances >= Deductions),
        CONSTRAINT CK_SalaryRecords_PaymentStatus
            CHECK (PaymentStatus IN (N'Pending', N'Paid', N'OnHold'))
    );
END
GO

CREATE INDEX IX_SalaryRecords_YearMonth
    ON dbo.SalaryRecords(SalaryYear, SalaryMonth);
GO

/* =========================
   5. OPTIONAL EMPLOYEE PERFORMANCE
   Bonus feature support.
   ========================= */
IF OBJECT_ID(N'dbo.EmployeePerformance', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeePerformance
    (
        PerformanceId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_EmployeePerformance PRIMARY KEY,
        EmployeeId INT NOT NULL,
        ReviewPeriodStart DATE NOT NULL,
        ReviewPeriodEnd DATE NOT NULL,
        Score DECIMAL(5,2) NOT NULL,
        GoalsAchieved NVARCHAR(1000) NULL,
        ReviewerUserId NVARCHAR(450) NULL,
        Comments NVARCHAR(1000) NULL,
        CreatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_EmployeePerformance_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_EmployeePerformance_Employees FOREIGN KEY (EmployeeId)
            REFERENCES dbo.Employees(EmployeeId),
        CONSTRAINT CK_EmployeePerformance_Score CHECK (Score BETWEEN 0 AND 100),
        CONSTRAINT CK_EmployeePerformance_Period CHECK (ReviewPeriodEnd >= ReviewPeriodStart)
    );
END
GO

CREATE INDEX IX_EmployeePerformance_Employee_Period
    ON dbo.EmployeePerformance(EmployeeId, ReviewPeriodStart, ReviewPeriodEnd);
GO

/* =========================
   6. STORED PROCEDURES
   ========================= */

CREATE OR ALTER PROCEDURE dbo.usp_Employee_GetAll
    @Search NVARCHAR(200) = NULL,
    @DepartmentId INT = NULL,
    @EmploymentStatus NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        e.EmployeeId,
        e.EmployeeCode,
        e.FirstName,
        e.LastName,
        e.Email,
        e.Phone,
        e.DepartmentId,
        d.DepartmentName,
        e.Designation,
        e.JoiningDate,
        e.Salary,
        e.EmploymentStatus,
        e.CreatedAt,
        e.UpdatedAt
    FROM dbo.Employees AS e
    INNER JOIN dbo.Departments AS d
        ON d.DepartmentId = e.DepartmentId
    WHERE
        (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
        AND (@EmploymentStatus IS NULL OR e.EmploymentStatus = @EmploymentStatus)
        AND
        (
            @Search IS NULL
            OR e.EmployeeCode LIKE N'%' + @Search + N'%'
            OR e.FirstName LIKE N'%' + @Search + N'%'
            OR e.LastName LIKE N'%' + @Search + N'%'
            OR e.Email LIKE N'%' + @Search + N'%'
        )
    ORDER BY e.EmployeeId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetSummary
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) FROM dbo.Employees) AS TotalEmployees,
        (SELECT COUNT(*) FROM dbo.Employees WHERE EmploymentStatus = N'Active') AS ActiveEmployees,
        (SELECT COUNT(*) FROM dbo.Departments WHERE IsActive = 1) AS ActiveDepartments,
        (SELECT COUNT(*) FROM dbo.Attendance
          WHERE AttendanceDate = CONVERT(date, GETDATE())
            AND AttendanceStatus = N'Present') AS PresentToday,
        (SELECT COUNT(*) FROM dbo.Attendance
          WHERE AttendanceDate = CONVERT(date, GETDATE())
            AND AttendanceStatus = N'Absent') AS AbsentToday;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_AttendanceMonthly
    @Year INT,
    @Month INT,
    @DepartmentId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        e.EmployeeId,
        e.EmployeeCode,
        CONCAT(e.FirstName, N' ', e.LastName) AS EmployeeName,
        d.DepartmentName,
        SUM(CASE WHEN a.AttendanceStatus = N'Present' THEN 1 ELSE 0 END) AS PresentDays,
        SUM(CASE WHEN a.AttendanceStatus = N'Absent' THEN 1 ELSE 0 END) AS AbsentDays,
        SUM(CASE WHEN a.AttendanceStatus = N'Leave' THEN 1 ELSE 0 END) AS LeaveDays,
        SUM(CASE WHEN a.AttendanceStatus = N'HalfDay' THEN 1 ELSE 0 END) AS HalfDays
    FROM dbo.Employees AS e
    INNER JOIN dbo.Departments AS d ON d.DepartmentId = e.DepartmentId
    LEFT JOIN dbo.Attendance AS a
        ON a.EmployeeId = e.EmployeeId
       AND YEAR(a.AttendanceDate) = @Year
       AND MONTH(a.AttendanceDate) = @Month
    WHERE @DepartmentId IS NULL OR e.DepartmentId = @DepartmentId
    GROUP BY e.EmployeeId, e.EmployeeCode, e.FirstName, e.LastName, d.DepartmentName
    ORDER BY d.DepartmentName, e.FirstName, e.LastName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_HiringTrend
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        MONTH(JoiningDate) AS [Month],
        DATENAME(MONTH, DATEFROMPARTS(@Year, MONTH(JoiningDate), 1)) AS MonthName,
        COUNT(*) AS NewHires
    FROM dbo.Employees
    WHERE JoiningDate >= DATEFROMPARTS(@Year, 1, 1)
      AND JoiningDate < DATEFROMPARTS(@Year + 1, 1, 1)
    GROUP BY MONTH(JoiningDate)
    ORDER BY [Month];
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_DepartmentEmployeeCount
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.DepartmentId,
        d.DepartmentName,
        COUNT(e.EmployeeId) AS TotalEmployees,
        SUM(CASE WHEN e.EmploymentStatus = N'Active' THEN 1 ELSE 0 END) AS ActiveEmployees
    FROM dbo.Departments AS d
    LEFT JOIN dbo.Employees AS e ON e.DepartmentId = d.DepartmentId
    GROUP BY d.DepartmentId, d.DepartmentName
    ORDER BY d.DepartmentName;
END
GO

/* =========================
   7. SAMPLE DEPARTMENTS
   Safe to re-run: avoids duplicates.
   ========================= */
IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE DepartmentName = N'IT')
    INSERT INTO dbo.Departments (DepartmentName, Description) VALUES (N'IT', N'Information Technology');
IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE DepartmentName = N'HR')
    INSERT INTO dbo.Departments (DepartmentName, Description) VALUES (N'HR', N'Human Resources');
IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE DepartmentName = N'Sales')
    INSERT INTO dbo.Departments (DepartmentName, Description) VALUES (N'Sales', N'Sales and Business Development');
IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE DepartmentName = N'Marketing')
    INSERT INTO dbo.Departments (DepartmentName, Description) VALUES (N'Marketing', N'Marketing and Communications');
IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE DepartmentName = N'Finance')
    INSERT INTO dbo.Departments (DepartmentName, Description) VALUES (N'Finance', N'Finance and Accounting');
GO

/* =========================
   8. OPTIONAL SAMPLE EMPLOYEES
   Uncomment this block only if you want demo data.
   =========================
DECLARE @IT INT = (SELECT DepartmentId FROM dbo.Departments WHERE DepartmentName = N'IT');
DECLARE @HR INT = (SELECT DepartmentId FROM dbo.Departments WHERE DepartmentName = N'HR');

IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeCode = N'EMP001')
BEGIN
    INSERT INTO dbo.Employees
        (EmployeeCode, FirstName, LastName, Email, Phone, DepartmentId, Designation, JoiningDate, Salary)
    VALUES
        (N'EMP001', N'Rahul', N'Sharma', N'rahul.demo@example.com', N'9000000001',
         @IT, N'Software Developer', '2026-01-15', 45000.00);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeCode = N'EMP002')
BEGIN
    INSERT INTO dbo.Employees
        (EmployeeCode, FirstName, LastName, Email, Phone, DepartmentId, Designation, JoiningDate, Salary)
    VALUES
        (N'EMP002', N'Neha', N'Singh', N'neha.demo@example.com', N'9000000002',
         @HR, N'HR Executive', '2026-02-01', 38000.00);
END;
GO
*/

/* =========================
   9. VERIFY DATABASE OBJECTS
   ========================= */
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

SELECT name AS StoredProcedureName
FROM sys.procedures
WHERE name IN
(
    N'usp_Employee_GetAll',
    N'usp_Dashboard_GetSummary',
    N'usp_Report_AttendanceMonthly',
    N'usp_Report_HiringTrend',
    N'usp_Report_DepartmentEmployeeCount'
)
ORDER BY name;
GO
