# ECMS

ECMS is a compact English Center Management System built with ASP.NET Core Razor Pages, Entity Framework Core, and SQL Server.

## Scope

- System login with role-based access
- Class management
- Schedule management with room and teacher conflict checks
- Attendance tracking for teachers
- Score entry and scoreboards for admin, teacher, and student roles

Out of scope:

- Tuition management
- Detailed HR management
- CRM and marketing

## Tech Stack

- ASP.NET Core Razor Pages (`net10.0`)
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity with hashed passwords and role-based authorization

## Project Structure

- [`ECMS.Web`](/workspaces/ECMS/ECMS.Web): main Razor Pages application
- [`ECMS.Web/Data`](/workspaces/ECMS/ECMS.Web/Data): `ApplicationDbContext`, seed data, migrations
- [`ECMS.Web/Pages`](/workspaces/ECMS/ECMS.Web/Pages): Razor Pages by module
- [`ECMS.Web/Models`](/workspaces/ECMS/ECMS.Web/Models): domain entities

## Setup

1. Update the SQL Server connection string in [`appsettings.json`](/workspaces/ECMS/ECMS.Web/appsettings.json).
2. Apply the initial migration:

```bash
dotnet dotnet-ef database update --project ECMS.Web/ECMS.Web.csproj
```

3. Run the web app:

```bash
dotnet run --project ECMS.Web/ECMS.Web.csproj
```

When SQL Server is reachable, the app also attempts to apply migrations and seed demo data on startup.

## Demo Accounts

- Admin: `admin / Admin@123`
- Staff: `staff / Staff@123`
- Teacher: `teacher.alice / Teacher@123`
- Student: `student.tom / Student@123`

## Main Modules

- Admin/Staff
  - Classes
  - Schedule management
  - Scoreboard view
- Teacher
  - My schedule
  - Attendance
  - Score entry
  - Scoreboard view
- Student
  - My schedule
  - My scores
