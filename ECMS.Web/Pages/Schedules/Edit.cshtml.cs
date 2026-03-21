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

[Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.Staff)]
public class EditModel(
    ApplicationDbContext context,
    ScheduleConflictService scheduleConflictService,
    ScheduleDateTimeService scheduleDateTimeService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public ScheduleFormModel Input { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }

    public string MinSelectableDate { get; private set; } = string.Empty;

    public string ActiveTimeZoneLabel { get; private set; } = string.Empty;

    public IEnumerable<SelectListItem> ClassOptions { get; private set; } = [];

    public IEnumerable<SelectListItem> RoomOptions { get; private set; } = [];

    public IEnumerable<SelectListItem> TeacherOptions { get; private set; } = [];

    public IEnumerable<SelectListItem> StatusOptions => Enum.GetValues<ScheduleStatus>()
        .Select(status => new SelectListItem(status.ToString(), status.ToString()));

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var timeZone = scheduleDateTimeService.ResolveTimeZone(HttpContext);
        InitialisePageMetadata(timeZone);

        var schedule = await context.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == Id, cancellationToken);

        if (schedule is null)
        {
            return NotFound();
        }

        if (scheduleDateTimeService.NormalizeUtc(schedule.StartAtUtc) <= DateTime.UtcNow)
        {
            ErrorMessage = "Only future sessions can be edited.";
            return RedirectToPage("/Schedules/Index");
        }

        var (localStart, localEnd) = scheduleDateTimeService.ConvertUtcToLocalRange(schedule.StartAtUtc, schedule.EndAtUtc, timeZone);

        Input = new ScheduleFormModel
        {
            ClassId = schedule.ClassId,
            TeacherId = schedule.TeacherId,
            ClassDate = localStart.Date,
            StartTime = localStart.TimeOfDay,
            EndTime = localEnd.TimeOfDay,
            RoomId = schedule.RoomId,
            Status = schedule.Status,
            TimeZoneId = timeZone.Id
        };

        await LoadSelectOptionsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var timeZone = scheduleDateTimeService.ResolveTimeZone(HttpContext, Input.TimeZoneId);
        Input.TimeZoneId = timeZone.Id;
        InitialisePageMetadata(timeZone);

        var schedule = await context.Schedules.FirstOrDefaultAsync(item => item.Id == Id, cancellationToken);
        if (schedule is null)
        {
            return NotFound();
        }

        if (scheduleDateTimeService.NormalizeUtc(schedule.StartAtUtc) <= DateTime.UtcNow)
        {
            ErrorMessage = "Only future sessions can be edited.";
            return RedirectToPage("/Schedules/Index");
        }

        if (!ModelState.IsValid)
        {
            await LoadSelectOptionsAsync(cancellationToken);
            return Page();
        }

        if (!TryBuildUtcRange(timeZone, out var startAtUtc, out var endAtUtc))
        {
            await LoadSelectOptionsAsync(cancellationToken);
            return Page();
        }

        var validationErrors = await scheduleConflictService.ValidateAsync(
            Input.ClassId,
            startAtUtc,
            endAtUtc,
            Input.RoomId,
            Input.TeacherId,
            scheduleId: Id,
            cancellationToken: cancellationToken);

        foreach (var error in validationErrors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        if (!ModelState.IsValid)
        {
            await LoadSelectOptionsAsync(cancellationToken);
            return Page();
        }

        schedule.ClassId = Input.ClassId;
        schedule.TeacherId = Input.TeacherId;
        schedule.StartAtUtc = startAtUtc;
        schedule.EndAtUtc = endAtUtc;
        schedule.RoomId = Input.RoomId;
        schedule.Status = Input.Status;

        await context.SaveChangesAsync(cancellationToken);
        return RedirectToPage("/Schedules/Index");
    }

    private async Task LoadSelectOptionsAsync(CancellationToken cancellationToken)
    {
        ClassOptions = await context.Classes
            .AsNoTracking()
            .OrderBy(courseClass => courseClass.ClassName)
            .Select(courseClass => new SelectListItem(courseClass.ClassName, courseClass.Id.ToString()))
            .ToListAsync(cancellationToken);

        RoomOptions = await context.Rooms
            .AsNoTracking()
            .Where(room => room.IsActive)
            .OrderBy(room => room.RoomName)
            .Select(room => new SelectListItem(room.RoomName, room.Id.ToString()))
            .ToListAsync(cancellationToken);

        TeacherOptions = await context.Teachers
            .AsNoTracking()
            .OrderBy(teacher => teacher.FullName)
            .Select(teacher => new SelectListItem(teacher.FullName, teacher.Id.ToString()))
            .ToListAsync(cancellationToken);
    }

    private void InitialisePageMetadata(TimeZoneInfo timeZone)
    {
        var localToday = scheduleDateTimeService.GetLocalToday(timeZone);
        MinSelectableDate = localToday.ToString("yyyy-MM-dd");
        ActiveTimeZoneLabel = scheduleDateTimeService.GetTimeZoneLabel(timeZone);
    }

    private bool TryBuildUtcRange(TimeZoneInfo timeZone, out DateTime startAtUtc, out DateTime endAtUtc)
    {
        startAtUtc = default;
        endAtUtc = default;

        var localToday = scheduleDateTimeService.GetLocalToday(timeZone);
        if (Input.ClassDate.Date < localToday)
        {
            ModelState.AddModelError(nameof(Input.ClassDate), "Only today or future dates can be scheduled.");
            return false;
        }

        if (Input.StartTime >= Input.EndTime)
        {
            ModelState.AddModelError(string.Empty, "Start time must be earlier than end time.");
            return false;
        }

        if (!scheduleDateTimeService.TryConvertLocalToUtc(Input.ClassDate, Input.StartTime, timeZone, out startAtUtc, out var startError))
        {
            ModelState.AddModelError(nameof(Input.StartTime), startError ?? "The selected start time is invalid.");
            return false;
        }

        if (!scheduleDateTimeService.TryConvertLocalToUtc(Input.ClassDate, Input.EndTime, timeZone, out endAtUtc, out var endError))
        {
            ModelState.AddModelError(nameof(Input.EndTime), endError ?? "The selected end time is invalid.");
            return false;
        }

        if (startAtUtc >= endAtUtc)
        {
            ModelState.AddModelError(string.Empty, "Start time must be earlier than end time.");
            return false;
        }

        if (startAtUtc < DateTime.UtcNow)
        {
            ModelState.AddModelError(nameof(Input.StartTime), "The session must start in the future.");
            return false;
        }

        return true;
    }
}
