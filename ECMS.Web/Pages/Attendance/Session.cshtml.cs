using ECMS.Web.Authorization;
using ECMS.Web.Data;
using ECMS.Web.Models;
using ECMS.Web.Services;
using ECMS.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Pages.Attendance;

[Authorize(Roles = ApplicationRoles.Teacher)]
public class SessionModel(
    ApplicationDbContext context,
    UserProfileService userProfileService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public List<AttendanceEntryRow> Entries { get; set; } = [];

    public SessionSummary? Session { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var teacher = await userProfileService.GetTeacherAsync(User, cancellationToken);
        if (teacher is null)
        {
            return Forbid();
        }

        var schedule = await LoadScheduleAsync(teacher.Id, cancellationToken);
        if (schedule is null)
        {
            return NotFound();
        }

        Session = new SessionSummary
        {
            Id = schedule.Id,
            ClassName = schedule.Class.ClassName,
            ClassDate = schedule.ClassDate,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            RoomName = schedule.Room.RoomName
        };

        Entries = schedule.Class.StudentClasses
            .OrderBy(studentClass => studentClass.Student.FullName)
            .Select(studentClass =>
            {
                var existing = schedule.Attendances.FirstOrDefault(attendance => attendance.StudentId == studentClass.StudentId);
                return new AttendanceEntryRow
                {
                    StudentId = studentClass.StudentId,
                    StudentCode = studentClass.Student.StudentCode,
                    StudentName = studentClass.Student.FullName,
                    Status = existing?.Status ?? AttendanceStatus.Present
                };
            })
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var teacher = await userProfileService.GetTeacherAsync(User, cancellationToken);
        if (teacher is null)
        {
            return Forbid();
        }

        var schedule = await context.Schedules
            .Include(item => item.Class)
                .ThenInclude(courseClass => courseClass.StudentClasses)
            .Include(item => item.Attendances)
            .FirstOrDefaultAsync(item => item.Id == Id && item.TeacherId == teacher.Id, cancellationToken);

        if (schedule is null)
        {
            return NotFound();
        }

        var validStudentIds = schedule.Class.StudentClasses.Select(studentClass => studentClass.StudentId).ToHashSet();

        foreach (var entry in Entries.Where(item => validStudentIds.Contains(item.StudentId)))
        {
            var existing = schedule.Attendances.FirstOrDefault(attendance => attendance.StudentId == entry.StudentId);
            if (existing is null)
            {
                context.Attendances.Add(new Models.Attendance
                {
                    ScheduleId = schedule.Id,
                    StudentId = entry.StudentId,
                    Status = entry.Status,
                    RecordedAtUtc = DateTime.UtcNow,
                    RecordedByUserId = teacher.ApplicationUserId ?? string.Empty
                });
            }
            else
            {
                existing.Status = entry.Status;
                existing.RecordedAtUtc = DateTime.UtcNow;
                existing.RecordedByUserId = teacher.ApplicationUserId ?? string.Empty;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return RedirectToPage("/Attendance/Index");
    }

    private async Task<Schedule?> LoadScheduleAsync(int teacherId, CancellationToken cancellationToken)
    {
        return await context.Schedules
            .AsNoTracking()
            .Include(schedule => schedule.Room)
            .Include(schedule => schedule.Class)
                .ThenInclude(courseClass => courseClass.StudentClasses)
                .ThenInclude(studentClass => studentClass.Student)
            .Include(schedule => schedule.Attendances)
            .FirstOrDefaultAsync(schedule => schedule.Id == Id && schedule.TeacherId == teacherId, cancellationToken);
    }

    public class SessionSummary
    {
        public int Id { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public DateTime ClassDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string RoomName { get; set; } = string.Empty;
    }
}
