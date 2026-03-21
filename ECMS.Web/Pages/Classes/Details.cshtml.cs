using ECMS.Web.Authorization;
using ECMS.Web.Data;
using ECMS.Web.Models;
using ECMS.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Pages.Classes;

[Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.Staff)]
public class DetailsModel(
    ApplicationDbContext context,
    Services.ScheduleDateTimeService scheduleDateTimeService) : PageModel
{
    public ClassDetailsViewModel? Class { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var courseClass = await context.Classes
            .AsNoTracking()
            .Include(item => item.Teacher)
            .Include(item => item.StudentClasses)
                .ThenInclude(studentClass => studentClass.Student)
            .Include(item => item.Schedules)
                .ThenInclude(schedule => schedule.Room)
            .Include(item => item.Schedules)
                .ThenInclude(schedule => schedule.Teacher)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (courseClass is null)
        {
            return NotFound();
        }

        var timeZone = scheduleDateTimeService.ResolveTimeZone(HttpContext);
        var nowUtc = DateTime.UtcNow;
        var nextSchedule = courseClass.Schedules
            .Where(schedule => scheduleDateTimeService.NormalizeUtc(schedule.StartAtUtc) >= nowUtc)
            .OrderBy(schedule => schedule.StartAtUtc)
            .FirstOrDefault();

        Class = new ClassDetailsViewModel
        {
            Id = courseClass.Id,
            ClassName = courseClass.ClassName,
            Level = courseClass.Level,
            TeacherId = courseClass.TeacherId,
            TeacherName = courseClass.Teacher != null ? courseClass.Teacher.FullName : "Unassigned",
            MaxStudents = courseClass.MaxStudents,
            StudentCount = courseClass.StudentClasses.Count,
            Status = courseClass.Status,
            NextSessionDate = nextSchedule is null
                ? null
                : scheduleDateTimeService.ConvertUtcToLocalRange(nextSchedule.StartAtUtc, nextSchedule.EndAtUtc, timeZone).LocalStart.Date,
            Students = courseClass.StudentClasses
                .OrderBy(studentClass => studentClass.Student.FullName)
                .Select(studentClass => new StudentItem
                {
                    Id = studentClass.StudentId,
                    StudentCode = studentClass.Student.StudentCode,
                    FullName = studentClass.Student.FullName,
                    Email = studentClass.Student.Email
                })
                .ToList(),
            Sessions = courseClass.Schedules
                .OrderByDescending(schedule => schedule.StartAtUtc)
                .ThenBy(schedule => schedule.EndAtUtc)
                .Take(6)
                .Select(schedule => scheduleDateTimeService.BuildScheduleListItem(schedule, timeZone, canModify: false))
                .ToList()
        };

        return Page();
    }

    public class ClassDetailsViewModel
    {
        public int Id { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public EnglishLevel Level { get; set; }

        public int? TeacherId { get; set; }

        public string TeacherName { get; set; } = string.Empty;

        public int MaxStudents { get; set; }

        public int StudentCount { get; set; }

        public ClassStatus Status { get; set; }

        public DateTime? NextSessionDate { get; set; }

        public List<StudentItem> Students { get; set; } = [];

        public List<ScheduleListItem> Sessions { get; set; } = [];
    }

    public class StudentItem
    {
        public int Id { get; set; }

        public string StudentCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }
    }
}
