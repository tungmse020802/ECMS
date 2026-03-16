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

- ASP.NET Core Razor Pages (`net8.0`)
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
For local development, you can also override it in [`appsettings.Development.json`](/workspaces/ECMS/ECMS.Web/appsettings.Development.json).
2. Run the web app:

```bash
dotnet run --project ECMS.Web/ECMS.Web.csproj
```

When the application starts, it will automatically:

- check whether the target database exists
- create the database if it does not exist
- apply pending EF Core migrations
- seed demo roles, users, classes, schedules, attendance, and scores

You do not need to run `dotnet ef database update` manually for normal setup.

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
