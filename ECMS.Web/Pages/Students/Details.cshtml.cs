using ECMS.Web.Authorization;
using ECMS.Web.Data;
using ECMS.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Pages.Students;

[Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.Staff)]
public class DetailsModel(ApplicationDbContext context) : PageModel
{
    public StudentDetailsViewModel? Student { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var student = await context.Students
            .AsNoTracking()
            .Include(item => item.ApplicationUser)
            .Include(item => item.StudentClasses)
                .ThenInclude(studentClass => studentClass.Class)
                .ThenInclude(courseClass => courseClass.Teacher)
            .Include(item => item.Scores)
                .ThenInclude(score => score.Class)
            .Include(item => item.Scores)
                .ThenInclude(score => score.ScoreType)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (student is null)
        {
            return NotFound();
        }

        Student = new StudentDetailsViewModel
        {
            Id = student.Id,
            StudentCode = student.StudentCode,
            FullName = student.FullName,
            Email = student.Email,
            PortalUserName = student.ApplicationUser?.UserName,
            EnrollmentCount = student.StudentClasses.Count,
            ScoreCount = student.Scores.Count,
            EnrolledClasses = student.StudentClasses
                .OrderBy(studentClass => studentClass.Class.ClassName)
                .Select(studentClass => new EnrolledClassItem
                {
                    ClassId = studentClass.ClassId,
                    ClassName = studentClass.Class.ClassName,
                    Level = studentClass.Class.Level,
                    TeacherName = studentClass.Class.Teacher != null
                        ? studentClass.Class.Teacher.FullName
                        : "Unassigned",
                    EnrolledAtUtc = studentClass.EnrolledAtUtc,
                    Status = studentClass.Class.Status
                })
                .ToList(),
            RecentScores = student.Scores
                .OrderByDescending(score => score.RecordedAtUtc)
                .Take(6)
                .Select(score => new ScoreItem
                {
                    ClassId = score.ClassId,
                    ClassName = score.Class.ClassName,
                    ScoreTypeName = score.ScoreType.Name,
                    Value = score.Value,
                    RecordedAtUtc = score.RecordedAtUtc
                })
                .ToList()
        };

        return Page();
    }

    public class StudentDetailsViewModel
    {
        public int Id { get; set; }

        public string StudentCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PortalUserName { get; set; }

        public int EnrollmentCount { get; set; }

        public int ScoreCount { get; set; }

        public List<EnrolledClassItem> EnrolledClasses { get; set; } = [];

        public List<ScoreItem> RecentScores { get; set; } = [];
    }

    public class EnrolledClassItem
    {
        public int ClassId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public EnglishLevel Level { get; set; }

        public string TeacherName { get; set; } = string.Empty;

        public DateTime EnrolledAtUtc { get; set; }

        public ClassStatus Status { get; set; }
    }

    public class ScoreItem
    {
        public int ClassId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public string ScoreTypeName { get; set; } = string.Empty;

        public decimal Value { get; set; }

        public DateTime RecordedAtUtc { get; set; }
    }
}
