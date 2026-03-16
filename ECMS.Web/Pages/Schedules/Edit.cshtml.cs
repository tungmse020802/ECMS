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
    ScheduleConflictService scheduleConflictService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public ScheduleFormModel Input { get; set; } = new();

    public IEnumerable<SelectListItem> ClassOptions { get; private set; } = [];

    public IEnumerable<SelectListItem> RoomOptions { get; private set; } = [];

    public IEnumerable<SelectListItem> TeacherOptions { get; private set; } = [];

    public IEnumerable<SelectListItem> StatusOptions => Enum.GetValues<ScheduleStatus>()
        .Select(status => new SelectListItem(status.ToString(), status.ToString()));

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var schedule = await context.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == Id, cancellationToken);

        if (schedule is null)
        {
            return NotFound();
        }

        Input = new ScheduleFormModel
        {
            ClassId = schedule.ClassId,
            TeacherId = schedule.TeacherId,
            ClassDate = schedule.ClassDate,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            RoomId = schedule.RoomId,
            Status = schedule.Status
        };

        await LoadSelectOptionsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var schedule = await context.Schedules.FirstOrDefaultAsync(item => item.Id == Id, cancellationToken);
        if (schedule is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadSelectOptionsAsync(cancellationToken);
            return Page();
        }

        var validationErrors = await scheduleConflictService.ValidateAsync(
            Input.ClassId,
            Input.ClassDate.Date,
            Input.StartTime,
            Input.EndTime,
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
        schedule.ClassDate = Input.ClassDate.Date;
        schedule.StartTime = Input.StartTime;
        schedule.EndTime = Input.EndTime;
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
}
