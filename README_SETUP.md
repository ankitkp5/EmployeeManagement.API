# EmployeeManagement.API setup

## Prerequisites
- Visual Studio with the ASP.NET and web development workload
- .NET 8 SDK (or update the project and package versions to another compatible installed .NET version)
- SQL Server and SSMS
- The `EmployeeManagementDB_Setup.sql` script has already been executed

## 1. Set the SQL Server connection
Open `appsettings.json` and replace `YOUR_SERVER_NAME` with your SQL Server instance name.
The supplied example uses Windows Authentication. If you use SQL Server Authentication, configure credentials securely rather than committing a password.

## 2. Restore packages and run
Open `EmployeeManagement.API.csproj` in Visual Studio, restore NuGet packages, then run the project.
For local development, Swagger should open automatically when the environment is Development.

## 3. Verify database tables
This starter expects the tables `dbo.Employees` and `dbo.Departments` from the provided database script. It does not run migrations for these two tables.

## 4. Test endpoints in Swagger
- GET `/api/Departments`
- POST `/api/Departments`
- GET `/api/Employees`
- POST `/api/Employees`
- GET `/api/Employees/{id}`
- PUT `/api/Employees/{id}`
- DELETE `/api/Employees/{id}`

POST employee example:
{
  "employeeCode": "EMP001",
  "firstName": "Rahul",
  "lastName": "Sharma",
  "email": "rahul.demo@example.com",
  "phone": "9000000001",
  "departmentId": 1,
  "designation": "Software Developer",
  "joiningDate": "2026-01-15",
  "salary": 45000,
  "employmentStatus": "Active"
}

POST department example:
{
  "departmentName": "Operations",
  "description": "Operations team"
}

## Notes
- This is the initial Employee/Department API setup, not the entire assignment.
- Authentication/authorization, attendance, salary endpoints, reports, and global exception handling are subsequent steps.
- Do not expose this unauthenticated starter API publicly.
