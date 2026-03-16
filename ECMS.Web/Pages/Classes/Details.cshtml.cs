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
public class DetailsModel(ApplicationDbContext context) : PageModel
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
            NextSessionDate = courseClass.Schedules
                .Where(schedule => schedule.ClassDate >= DateTime.UtcNow.Date)
                .OrderBy(schedule => schedule.ClassDate)
                .ThenBy(schedule => schedule.StartTime)
                .Select(schedule => (DateTime?)schedule.ClassDate)
                .FirstOrDefault(),
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
                .OrderByDescending(schedule => schedule.ClassDate)
                .ThenBy(schedule => schedule.StartTime)
                .Take(6)
                .Select(schedule => new ScheduleListItem
                {
                    Id = schedule.Id,
                    ClassId = schedule.ClassId,
                    ClassName = courseClass.ClassName,
                    ClassDate = schedule.ClassDate,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    RoomName = schedule.Room.RoomName,
                    TeacherName = schedule.Teacher.FullName,
                    Status = schedule.Status
                })
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
