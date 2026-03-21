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
    UserProfileService userProfileService,
    ScheduleDateTimeService scheduleDateTimeService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateTime? Date { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ViewMode { get; set; } = "Week";

    [BindProperty(SupportsGet = true)]
    public int? ClassId { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public bool CanManageSchedule => User.IsInRole(ApplicationRoles.Admin) || User.IsInRole(ApplicationRoles.Staff);

    public string BoardDescription { get; private set; } = "Browse schedules using date, week, and class filters.";

    public string RangeLabel { get; private set; } = string.Empty;

    public string ActiveTimeZoneLabel { get; private set; } = string.Empty;

    public string MinSelectableDate { get; private set; } = string.Empty;

    public DateTime LocalToday { get; private set; }

    public bool IsWeekView => ViewMode == "Week";

    public bool IsRangeView { get; private set; }

    public bool CanGoPrevious { get; private set; }

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
        DateTime? fromDate,
        DateTime? toDate,
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

        if (scheduleDateTimeService.NormalizeUtc(schedule.StartAtUtc) <= DateTime.UtcNow)
        {
            ErrorMessage = "Only future sessions can be cancelled.";
            return RedirectToCurrentView(date, fromDate, toDate, viewMode, classId);
        }

        schedule.Status = ScheduleStatus.Cancelled;
        await context.SaveChangesAsync(cancellationToken);
        StatusMessage = "Schedule cancelled successfully.";

        return RedirectToCurrentView(date, fromDate, toDate, viewMode, classId);
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        int id,
        DateTime? date,
        DateTime? fromDate,
        DateTime? toDate,
        string? viewMode,
        int? classId,
        CancellationToken cancellationToken)
    {
        if (!CanManageSchedule)
        {
            return Forbid();
        }

        var schedule = await context.Schedules
            .Include(item => item.Attendances)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (schedule is null)
        {
            return NotFound();
        }

        if (scheduleDateTimeService.NormalizeUtc(schedule.StartAtUtc) <= DateTime.UtcNow)
        {
            ErrorMessage = "Only future sessions can be deleted.";
            return RedirectToCurrentView(date, fromDate, toDate, viewMode, classId);
        }

        context.Schedules.Remove(schedule);
        await context.SaveChangesAsync(cancellationToken);

        StatusMessage = "Schedule deleted successfully.";
        return RedirectToCurrentView(date, fromDate, toDate, viewMode, classId);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var timeZone = scheduleDateTimeService.ResolveTimeZone(HttpContext);
        var localToday = scheduleDateTimeService.GetLocalToday(timeZone);
        var selectedDate = (Date ?? localToday).Date;

        if (selectedDate < localToday)
        {
            selectedDate = localToday;
        }

        Date = selectedDate;
        ViewMode = string.Equals(ViewMode, "Day", StringComparison.OrdinalIgnoreCase) ? "Day" : "Week";
        ActiveTimeZoneLabel = scheduleDateTimeService.GetTimeZoneLabel(timeZone);
        MinSelectableDate = localToday.ToString("yyyy-MM-dd");
        LocalToday = localToday;

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

        if (FromDate.HasValue || ToDate.HasValue)
        {
            var rangeStartLocal = (FromDate ?? selectedDate).Date;
            if (rangeStartLocal < localToday)
            {
                rangeStartLocal = localToday;
            }

            var rangeEndLocal = (ToDate ?? rangeStartLocal).Date;
            if (rangeEndLocal < rangeStartLocal)
            {
                rangeEndLocal = rangeStartLocal;
            }

            FromDate = rangeStartLocal;
            ToDate = rangeEndLocal;
            IsRangeView = true;

            ApplyExplicitRange(
                scheduleQuery,
                timeZone,
                rangeStartLocal,
                rangeEndLocal,
                out scheduleQuery,
                out var rangeLabel,
                out var visibleDates);

            RangeLabel = rangeLabel;
            VisibleDates = visibleDates;
            PreviousRangeText = string.Empty;
            NextRangeText = string.Empty;
            CanGoPrevious = false;
        }
        else if (ViewMode == "Day")
        {
            var (rangeStartUtc, rangeEndUtc) = scheduleDateTimeService.GetUtcRangeForLocalDates(selectedDate, selectedDate, timeZone);
            scheduleQuery = scheduleQuery.Where(schedule => schedule.StartAtUtc >= rangeStartUtc && schedule.StartAtUtc < rangeEndUtc);
            RangeLabel = selectedDate.ToString("dd MMM yyyy");
            VisibleDates = [selectedDate];
            PreviousRangeText = selectedDate.AddDays(-1).ToString("yyyy-MM-dd");
            NextRangeText = selectedDate.AddDays(1).ToString("yyyy-MM-dd");
            CanGoPrevious = selectedDate > localToday;
        }
        else
        {
            var visibleStart = weekStart < localToday ? localToday : weekStart;
            var (rangeStartUtc, rangeEndUtc) = scheduleDateTimeService.GetUtcRangeForLocalDates(visibleStart, weekEnd, timeZone);

            scheduleQuery = scheduleQuery.Where(schedule => schedule.StartAtUtc >= rangeStartUtc && schedule.StartAtUtc < rangeEndUtc);
            RangeLabel = $"{visibleStart:dd MMM yyyy} - {weekEnd:dd MMM yyyy}";
            VisibleDates = Enumerable.Range(0, (weekEnd - visibleStart).Days + 1)
                .Select(offset => visibleStart.AddDays(offset))
                .ToList();
            PreviousRangeText = weekStart.AddDays(-7).ToString("yyyy-MM-dd");
            NextRangeText = weekStart.AddDays(7).ToString("yyyy-MM-dd");
            CanGoPrevious = weekStart > localToday;
        }

        var schedules = await scheduleQuery
            .OrderBy(schedule => schedule.StartAtUtc)
            .ThenBy(schedule => schedule.EndAtUtc)
            .ToListAsync(cancellationToken);

        Sessions = schedules
            .Select(schedule => scheduleDateTimeService.BuildScheduleListItem(
                schedule,
                timeZone,
                CanManageSchedule && scheduleDateTimeService.NormalizeUtc(schedule.StartAtUtc) > DateTime.UtcNow))
            .ToList();
    }

    private void ApplyExplicitRange(
        IQueryable<Schedule> sourceQuery,
        TimeZoneInfo timeZone,
        DateTime rangeStartLocal,
        DateTime rangeEndLocal,
        out IQueryable<Schedule> filteredQuery,
        out string rangeLabel,
        out List<DateTime> visibleDates)
    {
        var (rangeStartUtc, rangeEndUtc) = scheduleDateTimeService.GetUtcRangeForLocalDates(rangeStartLocal, rangeEndLocal, timeZone);
        filteredQuery = sourceQuery.Where(schedule => schedule.StartAtUtc >= rangeStartUtc && schedule.StartAtUtc < rangeEndUtc);
        rangeLabel = $"{rangeStartLocal:dd MMM yyyy} - {rangeEndLocal:dd MMM yyyy}";
        visibleDates = Enumerable.Range(0, (rangeEndLocal - rangeStartLocal).Days + 1)
            .Select(offset => rangeStartLocal.AddDays(offset))
            .ToList();
    }

    private RedirectToPageResult RedirectToCurrentView(
        DateTime? date,
        DateTime? fromDate,
        DateTime? toDate,
        string? viewMode,
        int? classId)
    {
        return RedirectToPage(new
        {
            Date = date?.ToString("yyyy-MM-dd"),
            FromDate = fromDate?.ToString("yyyy-MM-dd"),
            ToDate = toDate?.ToString("yyyy-MM-dd"),
            ViewMode = viewMode ?? "Week",
            ClassId = classId
        });
    }
}
