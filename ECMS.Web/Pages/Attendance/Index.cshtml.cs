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
    UserProfileService userProfileService) : PageModel
{
    public List<AttendanceSessionRow> Sessions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var teacher = await userProfileService.GetTeacherAsync(User, cancellationToken);
        if (teacher is null)
        {
            return Forbid();
        }

        Sessions = await context.Schedules
            .AsNoTracking()
            .Where(schedule => schedule.TeacherId == teacher.Id)
            .OrderByDescending(schedule => schedule.ClassDate)
            .ThenBy(schedule => schedule.StartTime)
            .Select(schedule => new AttendanceSessionRow
            {
                Id = schedule.Id,
                ClassDate = schedule.ClassDate,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                ClassName = schedule.Class.ClassName,
                RoomName = schedule.Room.RoomName,
                StudentCount = schedule.Class.StudentClasses.Count,
                RecordedCount = schedule.Attendances.Count
            })
            .ToListAsync(cancellationToken);

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
