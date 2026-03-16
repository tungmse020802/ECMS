using ECMS.Web.Authorization;
using ECMS.Web.Data;
using ECMS.Web.Models;
using ECMS.Web.Services;
using ECMS.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Pages.Schedules;

[Authorize]
public class IndexModel(
    ApplicationDbContext context,
    UserProfileService userProfileService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateTime? Date { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ViewMode { get; set; } = "Week";

    [BindProperty(SupportsGet = true)]
    public int? ClassId { get; set; }

    public bool CanManageSchedule => User.IsInRole(ApplicationRoles.Admin) || User.IsInRole(ApplicationRoles.Staff);

    public string BoardDescription { get; private set; } = "Browse schedules using date, week, and class filters.";

    public string RangeLabel { get; private set; } = string.Empty;

    public bool IsWeekView => ViewMode == "Week";

    public List<DateTime> VisibleDates { get; private set; } = [];

    public string PreviousRangeText { get; private set; } = string.Empty;

    public string NextRangeText { get; private set; } = string.Empty;

    public List<SelectListItem> ClassOptions { get; private set; } = [];

    public List<ScheduleListItem> Sessions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCancelAsync(
        int id,
        DateTime? date,
        string? viewMode,
        int? classId,
        CancellationToken cancellationToken)
    {
        if (!CanManageSchedule)
        {
            return Forbid();
        }

        var schedule = await context.Schedules.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (schedule is null)
        {
            return NotFound();
        }

        schedule.Status = ScheduleStatus.Cancelled;
        await context.SaveChangesAsync(cancellationToken);

        return RedirectToPage(new
        {
            Date = date?.ToString("yyyy-MM-dd"),
            ViewMode = viewMode ?? "Week",
            ClassId = classId
        });
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var selectedDate = (Date ?? DateTime.UtcNow.Date).Date;
        ViewMode = string.Equals(ViewMode, "Day", StringComparison.OrdinalIgnoreCase) ? "Day" : "Week";

        var weekOffset = ((7 + (int)selectedDate.DayOfWeek - (int)DayOfWeek.Monday) % 7);
        var weekStart = selectedDate.AddDays(-weekOffset);
        var weekEnd = weekStart.AddDays(6);

        IQueryable<CourseClass> classQuery = context.Classes.AsNoTracking();
        IQueryable<Schedule> scheduleQuery = context.Schedules
            .AsNoTracking()
            .Include(schedule => schedule.Class)
            .Include(schedule => schedule.Room)
            .Include(schedule => schedule.Teacher);

        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            var teacher = await userProfileService.GetTeacherAsync(User, cancellationToken);
            if (teacher is null)
            {
                Sessions = [];
                return;
            }

            BoardDescription = "Review the teaching sessions assigned to you.";
            classQuery = classQuery.Where(courseClass => courseClass.TeacherId == teacher.Id);
            scheduleQuery = scheduleQuery.Where(schedule => schedule.TeacherId == teacher.Id);
        }
        else if (User.IsInRole(ApplicationRoles.Student))
        {
            var student = await userProfileService.GetStudentAsync(User, cancellationToken);
            if (student is null)
            {
                Sessions = [];
                return;
            }

            BoardDescription = "Review the study schedule for the class or classes you are enrolled in.";
            classQuery = classQuery.Where(courseClass =>
                courseClass.StudentClasses.Any(studentClass => studentClass.StudentId == student.Id));
            scheduleQuery = scheduleQuery.Where(schedule =>
                schedule.Class.StudentClasses.Any(studentClass => studentClass.StudentId == student.Id));
        }
        else
        {
            BoardDescription = "Browse and manage schedules for all classes in the center.";
        }

        ClassOptions = await classQuery
            .OrderBy(courseClass => courseClass.ClassName)
            .Select(courseClass => new SelectListItem(courseClass.ClassName, courseClass.Id.ToString()))
            .ToListAsync(cancellationToken);

        if (ClassId.HasValue)
        {
            scheduleQuery = scheduleQuery.Where(schedule => schedule.ClassId == ClassId.Value);
        }

        if (ViewMode == "Day")
        {
            scheduleQuery = scheduleQuery.Where(schedule => schedule.ClassDate == selectedDate);
            RangeLabel = selectedDate.ToString("dd MMM yyyy");
            VisibleDates = [selectedDate];
            PreviousRangeText = selectedDate.AddDays(-1).ToString("yyyy-MM-dd");
            NextRangeText = selectedDate.AddDays(1).ToString("yyyy-MM-dd");
        }
        else
        {
            scheduleQuery = scheduleQuery.Where(schedule => schedule.ClassDate >= weekStart && schedule.ClassDate <= weekEnd);
            RangeLabel = $"{weekStart:dd MMM yyyy} - {weekEnd:dd MMM yyyy}";
            VisibleDates = Enumerable.Range(0, 7)
                .Select(offset => weekStart.AddDays(offset))
                .ToList();
            PreviousRangeText = weekStart.AddDays(-7).ToString("yyyy-MM-dd");
            NextRangeText = weekStart.AddDays(7).ToString("yyyy-MM-dd");
        }

        Sessions = await scheduleQuery
            .OrderBy(schedule => schedule.ClassDate)
            .ThenBy(schedule => schedule.StartTime)
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
}
