using ECMS.Web.Authorization;
using ECMS.Web.Data;
using ECMS.Web.Models;
using ECMS.Web.Services;
using ECMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Pages;

public class IndexModel(
    ApplicationDbContext context,
    UserProfileService userProfileService) : PageModel
{
    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

    public string DisplayName { get; private set; } = "User";

    public int ActiveClassCount { get; private set; }

    public int StudentCount { get; private set; }

    public int WeeklySessionCount { get; private set; }

    public List<DashboardCard> DashboardCards { get; private set; } = [];

    public List<ScheduleListItem> UpcomingSchedules { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (!IsAuthenticated)
        {
            return;
        }

        var currentUser = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.UserName == User.Identity!.Name, cancellationToken);

        DisplayName = currentUser?.FullName ?? User.Identity!.Name ?? "User";

        var today = DateTime.UtcNow.Date;
        var weekEnd = today.AddDays(7);

        IQueryable<Schedule> scheduleQuery = context.Schedules
            .AsNoTracking()
            .Include(schedule => schedule.Class)
            .Include(schedule => schedule.Room)
            .Include(schedule => schedule.Teacher)
            .Where(schedule => schedule.Status == ScheduleStatus.Scheduled);

        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            var teacher = await userProfileService.GetTeacherAsync(User, cancellationToken);
            if (teacher is null)
            {
                return;
            }

            ActiveClassCount = await context.Classes
                .AsNoTracking()
                .CountAsync(courseClass => courseClass.TeacherId == teacher.Id, cancellationToken);

            StudentCount = await context.StudentClasses
                .AsNoTracking()
                .Where(studentClass => studentClass.Class.TeacherId == teacher.Id)
                .Select(studentClass => studentClass.StudentId)
                .Distinct()
                .CountAsync(cancellationToken);

            scheduleQuery = scheduleQuery.Where(schedule => schedule.TeacherId == teacher.Id);

            DashboardCards =
            [
                new DashboardCard("My Timetable", "Teaching plan", "Review upcoming classes in weekly timetable format.", "/Schedules/Index"),
                new DashboardCard("Attendance", "UC05", "Mark students as present, absent, or late for each session.", "/Attendance/Index"),
                new DashboardCard("Score Entry", "UC06", "Record homework, quiz, midterm, and final scores.", "/Scores/Entry"),
                new DashboardCard("Scoreboard", "UC07", "See scoreboards for the classes assigned to you.", "/Scores/Index")
            ];
        }
        else if (User.IsInRole(ApplicationRoles.Student))
        {
            var student = await userProfileService.GetStudentAsync(User, cancellationToken);
            if (student is null)
            {
                return;
            }

            ActiveClassCount = await context.StudentClasses
                .AsNoTracking()
                .Where(studentClass => studentClass.StudentId == student.Id)
                .Select(studentClass => studentClass.ClassId)
                .Distinct()
                .CountAsync(cancellationToken);

            StudentCount = 1;

            scheduleQuery = scheduleQuery.Where(schedule =>
                schedule.Class.StudentClasses.Any(studentClass => studentClass.StudentId == student.Id));

            DashboardCards =
            [
                new DashboardCard("My Timetable", "UC04", "View the study calendar for your enrolled classes.", "/Schedules/Index"),
                new DashboardCard("My Scores", "UC07", "Track homework, quizzes, exams, and average score.", "/Scores/Index")
            ];
        }
        else
        {
            ActiveClassCount = await context.Classes
                .AsNoTracking()
                .CountAsync(cancellationToken);

            StudentCount = await context.Students
                .AsNoTracking()
                .CountAsync(cancellationToken);

            DashboardCards =
            [
                new DashboardCard("Classes", "UC02", "Create classes, assign teachers, and review rosters.", "/Classes/Index"),
                new DashboardCard("Timetable Mgmt", "UC03", "Create, update, and cancel teaching sessions.", "/Schedules/Create"),
                new DashboardCard("View Timetable", "UC04", "Browse classes in day or week timetable format.", "/Schedules/Index"),
                new DashboardCard("Scoreboard", "UC07", "Review scoreboards for every class and student.", "/Scores/Index")
            ];
        }

        WeeklySessionCount = await scheduleQuery.CountAsync(
            schedule => schedule.ClassDate >= today && schedule.ClassDate < weekEnd,
            cancellationToken);

        UpcomingSchedules = await scheduleQuery
            .Where(schedule => schedule.ClassDate >= today)
            .OrderBy(schedule => schedule.ClassDate)
            .ThenBy(schedule => schedule.StartTime)
            .Take(5)
            .Select(schedule => new ScheduleListItem
            {
                Id = schedule.Id,
                ClassId = schedule.ClassId,
                ClassName = schedule.Class.ClassName,
                ClassDate = schedule.ClassDate,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                RoomName = schedule.Room.RoomName,
                TeacherName = schedule.Teacher.FullName,
                Status = schedule.Status
            })
            .ToListAsync(cancellationToken);
    }

    public record DashboardCard(string Title, string Caption, string Description, string PagePath);
}
