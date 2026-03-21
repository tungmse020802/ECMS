using ECMS.Web.Authorization;
using ECMS.Web.Data;
using ECMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Pages.Attendance;

[Authorize(Roles = ApplicationRoles.Teacher)]
public class IndexModel(
    ApplicationDbContext context,
    UserProfileService userProfileService,
    ScheduleDateTimeService scheduleDateTimeService) : PageModel
{
    public List<AttendanceSessionRow> Sessions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var teacher = await userProfileService.GetTeacherAsync(User, cancellationToken);
        if (teacher is null)
        {
            return Forbid();
        }

        var timeZone = scheduleDateTimeService.ResolveTimeZone(HttpContext);

        var schedules = await context.Schedules
            .AsNoTracking()
            .Where(schedule => schedule.TeacherId == teacher.Id)
            .Include(schedule => schedule.Class)
                .ThenInclude(courseClass => courseClass.StudentClasses)
            .Include(schedule => schedule.Room)
            .Include(schedule => schedule.Attendances)
            .OrderByDescending(schedule => schedule.StartAtUtc)
            .ThenBy(schedule => schedule.EndAtUtc)
            .ToListAsync(cancellationToken);

        Sessions = schedules
            .Select(schedule =>
            {
                var (localStart, localEnd) = scheduleDateTimeService.ConvertUtcToLocalRange(schedule.StartAtUtc, schedule.EndAtUtc, timeZone);

                return new AttendanceSessionRow
                {
                    Id = schedule.Id,
                    ClassDate = localStart.Date,
                    StartTime = localStart.TimeOfDay,
                    EndTime = localEnd.TimeOfDay,
                    ClassName = schedule.Class.ClassName,
                    RoomName = schedule.Room.RoomName,
                    StudentCount = schedule.Class.StudentClasses.Count,
                    RecordedCount = schedule.Attendances.Count
                };
            })
            .ToList();

        return Page();
    }

    public class AttendanceSessionRow
    {
        public int Id { get; set; }

        public DateTime ClassDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public string RoomName { get; set; } = string.Empty;

        public int StudentCount { get; set; }

        public int RecordedCount { get; set; }
    }
}
