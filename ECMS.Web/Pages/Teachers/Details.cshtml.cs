using ECMS.Web.Authorization;
using ECMS.Web.Data;
using ECMS.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Pages.Teachers;

[Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.Staff)]
public class DetailsModel(
    ApplicationDbContext context,
    Services.ScheduleDateTimeService scheduleDateTimeService) : PageModel
{
    public TeacherDetailsViewModel? Teacher { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        var teacher = await context.Teachers
            .AsNoTracking()
            .Include(item => item.ApplicationUser)
            .Include(item => item.Classes)
                .ThenInclude(courseClass => courseClass.StudentClasses)
            .Include(item => item.Schedules)
                .ThenInclude(schedule => schedule.Class)
            .Include(item => item.Schedules)
                .ThenInclude(schedule => schedule.Room)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (teacher is null)
        {
            return NotFound();
        }

        var timeZone = scheduleDateTimeService.ResolveTimeZone(HttpContext);

        Teacher = new TeacherDetailsViewModel
        {
            Id = teacher.Id,
            FullName = teacher.FullName,
            Email = teacher.Email,
            PortalUserName = teacher.ApplicationUser?.UserName,
            ClassCount = teacher.Classes.Count,
            UpcomingSessionCount = teacher.Schedules.Count(schedule =>
                schedule.Status == ScheduleStatus.Scheduled &&
                schedule.StartAtUtc >= nowUtc),
            Classes = teacher.Classes
                .OrderBy(courseClass => courseClass.ClassName)
                .Select(courseClass => new ClassListItem
                {
                    Id = courseClass.Id,
                    ClassName = courseClass.ClassName,
                    Level = courseClass.Level,
                    StudentCount = courseClass.StudentClasses.Count,
                    MaxStudents = courseClass.MaxStudents,
                    Status = courseClass.Status
                })
                .ToList(),
            UpcomingSessions = teacher.Schedules
                .Where(schedule => scheduleDateTimeService.NormalizeUtc(schedule.StartAtUtc) >= nowUtc)
                .OrderBy(schedule => schedule.StartAtUtc)
                .ThenBy(schedule => schedule.EndAtUtc)
                .Take(6)
                .Select(schedule =>
                {
                    var (localStart, localEnd) = scheduleDateTimeService.ConvertUtcToLocalRange(schedule.StartAtUtc, schedule.EndAtUtc, timeZone);

                    return new SessionListItem
                    {
                        Id = schedule.Id,
                        ClassId = schedule.ClassId,
                        ClassName = schedule.Class.ClassName,
                        ClassDate = localStart.Date,
                        StartTime = localStart.TimeOfDay,
                        EndTime = localEnd.TimeOfDay,
                        RoomName = schedule.Room.RoomName,
                        Status = schedule.Status
                    };
                })
                .ToList()
        };

        return Page();
    }

    public class TeacherDetailsViewModel
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PortalUserName { get; set; }

        public int ClassCount { get; set; }

        public int UpcomingSessionCount { get; set; }

        public List<ClassListItem> Classes { get; set; } = [];

        public List<SessionListItem> UpcomingSessions { get; set; } = [];
    }

    public class ClassListItem
    {
        public int Id { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public EnglishLevel Level { get; set; }

        public int StudentCount { get; set; }

        public int MaxStudents { get; set; }

        public ClassStatus Status { get; set; }
    }

    public class SessionListItem
    {
        public int Id { get; set; }

        public int ClassId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public DateTime ClassDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string RoomName { get; set; } = string.Empty;

        public ScheduleStatus Status { get; set; }
    }
}
