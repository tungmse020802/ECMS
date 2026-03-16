using ECMS.Web.Authorization;
using ECMS.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Data;

public static class SeedData
{
    public static async Task InitialiseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("SeedData");

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var roleName in ApplicationRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = roleName
                    });
                }
            }

            var adminUser = await EnsureUserAsync(
                userManager,
                "admin",
                "admin@ecms.local",
                "ECMS Administrator",
                "Admin@123",
                [ApplicationRoles.Admin]);

            await EnsureUserAsync(
                userManager,
                "staff",
                "staff@ecms.local",
                "Front Desk Staff",
                "Staff@123",
                [ApplicationRoles.Staff]);

            var teacherUser = await EnsureUserAsync(
                userManager,
                "teacher.alice",
                "alice.teacher@ecms.local",
                "Alice Nguyen",
                "Teacher@123",
                [ApplicationRoles.Teacher]);

            var studentUser = await EnsureUserAsync(
                userManager,
                "student.tom",
                "tom.student@ecms.local",
                "Tom Tran",
                "Student@123",
                [ApplicationRoles.Student]);

            if (!await context.Teachers.AnyAsync())
            {
                context.Teachers.AddRange(
                    new Teacher
                    {
                        FullName = "Alice Nguyen",
                        Email = "alice.teacher@ecms.local",
                        ApplicationUserId = teacherUser.Id
                    },
                    new Teacher
                    {
                        FullName = "Brian Le",
                        Email = "brian.teacher@ecms.local"
                    });

                await context.SaveChangesAsync();
            }

            if (!await context.Students.AnyAsync())
            {
                context.Students.AddRange(
                    new Student
                    {
                        StudentCode = "STD001",
                        FullName = "Tom Tran",
                        Email = "tom.student@ecms.local",
                        ApplicationUserId = studentUser.Id
                    },
                    new Student
                    {
                        StudentCode = "STD002",
                        FullName = "Linh Pham",
                        Email = "linh.student@ecms.local"
                    },
                    new Student
                    {
                        StudentCode = "STD003",
                        FullName = "Minh Vo",
                        Email = "minh.student@ecms.local"
                    });

                await context.SaveChangesAsync();
            }

            if (!await context.Rooms.AnyAsync())
            {
                context.Rooms.AddRange(
                    new Room { RoomName = "R101", Capacity = 20, IsActive = true },
                    new Room { RoomName = "R202", Capacity = 25, IsActive = true },
                    new Room { RoomName = "Lab-1", Capacity = 15, IsActive = true });

                await context.SaveChangesAsync();
            }

            if (!await context.ScoreTypes.AnyAsync())
            {
                context.ScoreTypes.AddRange(
                    new ScoreType { Name = "Homework" },
                    new ScoreType { Name = "Quiz" },
                    new ScoreType { Name = "Midterm" },
                    new ScoreType { Name = "Final" });

                await context.SaveChangesAsync();
            }

            if (!await context.Classes.AnyAsync())
            {
                var teachers = await context.Teachers.OrderBy(teacher => teacher.Id).ToListAsync();

                context.Classes.AddRange(
                    new CourseClass
                    {
                        ClassName = "Starter Morning A1",
                        Level = EnglishLevel.Starter,
                        TeacherId = teachers[0].Id,
                        MaxStudents = 20,
                        Status = ClassStatus.Active
                    },
                    new CourseClass
                    {
                        ClassName = "Intermediate Evening B1",
                        Level = EnglishLevel.Intermediate,
                        TeacherId = teachers[1].Id,
                        MaxStudents = 18,
                        Status = ClassStatus.Active
                    });

                await context.SaveChangesAsync();
            }

            if (!await context.StudentClasses.AnyAsync())
            {
                var firstClass = await context.Classes.OrderBy(courseClass => courseClass.Id).FirstAsync();
                var students = await context.Students.OrderBy(student => student.Id).ToListAsync();

                context.StudentClasses.AddRange(
                    students.Select(student => new StudentClass
                    {
                        StudentId = student.Id,
                        ClassId = firstClass.Id,
                        EnrolledAtUtc = DateTime.UtcNow
                    }));

                await context.SaveChangesAsync();
            }

            if (!await context.Schedules.AnyAsync())
            {
                var classes = await context.Classes.OrderBy(courseClass => courseClass.Id).ToListAsync();
                var rooms = await context.Rooms.OrderBy(room => room.Id).ToListAsync();
                var teachers = await context.Teachers.OrderBy(teacher => teacher.Id).ToListAsync();
                var nextMonday = DateTime.UtcNow.Date.AddDays(((int) DayOfWeek.Monday - (int) DateTime.UtcNow.DayOfWeek + 7) % 7);

                context.Schedules.AddRange(
                    new Schedule
                    {
                        ClassId = classes[0].Id,
                        ClassDate = nextMonday,
                        StartTime = new TimeSpan(8, 0, 0),
                        EndTime = new TimeSpan(10, 0, 0),
                        RoomId = rooms[0].Id,
                        TeacherId = teachers[0].Id,
                        Status = ScheduleStatus.Scheduled
                    },
                    new Schedule
                    {
                        ClassId = classes[0].Id,
                        ClassDate = nextMonday.AddDays(2),
                        StartTime = new TimeSpan(8, 0, 0),
                        EndTime = new TimeSpan(10, 0, 0),
                        RoomId = rooms[0].Id,
                        TeacherId = teachers[0].Id,
                        Status = ScheduleStatus.Scheduled
                    },
                    new Schedule
                    {
                        ClassId = classes[1].Id,
                        ClassDate = nextMonday.AddDays(1),
                        StartTime = new TimeSpan(18, 30, 0),
                        EndTime = new TimeSpan(20, 0, 0),
                        RoomId = rooms[1].Id,
                        TeacherId = teachers[1].Id,
                        Status = ScheduleStatus.Scheduled
                    });

                await context.SaveChangesAsync();
            }

            if (!await context.Scores.AnyAsync())
            {
                var teacher = await context.Teachers.FirstAsync(teacher => teacher.ApplicationUserId == teacherUser.Id);
                var courseClass = await context.Classes.OrderBy(courseClass => courseClass.Id).FirstAsync();
                var student = await context.Students.FirstAsync(student => student.ApplicationUserId == studentUser.Id);
                var scoreTypes = await context.ScoreTypes.OrderBy(scoreType => scoreType.Id).ToListAsync();

                context.Scores.AddRange(
                    new Score
                    {
                        StudentId = student.Id,
                        ClassId = courseClass.Id,
                        ScoreTypeId = scoreTypes[0].Id,
                        Value = 8.5m,
                        TeacherId = teacher.Id,
                        RecordedAtUtc = DateTime.UtcNow
                    },
                    new Score
                    {
                        StudentId = student.Id,
                        ClassId = courseClass.Id,
                        ScoreTypeId = scoreTypes[1].Id,
                        Value = 9.0m,
                        TeacherId = teacher.Id,
                        RecordedAtUtc = DateTime.UtcNow
                    });

                await context.SaveChangesAsync();
            }

            if (!await context.Attendances.AnyAsync())
            {
                var firstSchedule = await context.Schedules.OrderBy(schedule => schedule.Id).FirstAsync();
                var students = await context.StudentClasses
                    .Where(studentClass => studentClass.ClassId == firstSchedule.ClassId)
                    .Select(studentClass => studentClass.Student)
                    .ToListAsync();

                context.Attendances.AddRange(
                    students.Select(student => new Attendance
                    {
                        ScheduleId = firstSchedule.Id,
                        StudentId = student.Id,
                        Status = student.ApplicationUserId == studentUser.Id
                            ? AttendanceStatus.Present
                            : AttendanceStatus.Late,
                        RecordedAtUtc = DateTime.UtcNow,
                        RecordedByUserId = adminUser.Id
                    }));

                await context.SaveChangesAsync();
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Database initialization was skipped. Configure SQL Server and run migrations when the database is available.");
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string userName,
        string email,
        string fullName,
        string password,
        IEnumerable<string> roles)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to seed user '{userName}': {errors}");
            }
        }

        foreach (var role in roles)
        {
            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }

        return user;
    }
}
