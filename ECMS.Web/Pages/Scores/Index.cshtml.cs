using ECMS.Web.Authorization;
using ECMS.Web.Data;
using ECMS.Web.Services;
using ECMS.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Pages.Scores;

[Authorize]
public class IndexModel(
    ApplicationDbContext context,
    UserProfileService userProfileService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? ClassId { get; set; }

    public string BoardDescription { get; private set; } = "Review scoreboards based on your role.";

    public List<SelectListItem> ClassOptions { get; private set; } = [];

    public List<ScoreboardRow> Rows { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        IQueryable<Models.CourseClass> accessibleClasses = context.Classes.AsNoTracking();
        IQueryable<Models.StudentClass> accessibleStudentClasses = context.StudentClasses.AsNoTracking();

        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            var teacher = await userProfileService.GetTeacherAsync(User, cancellationToken);
            if (teacher is null)
            {
                return Forbid();
            }

            BoardDescription = "View scoreboards for the classes assigned to you.";
            accessibleClasses = accessibleClasses.Where(courseClass => courseClass.TeacherId == teacher.Id);
            accessibleStudentClasses = accessibleStudentClasses.Where(studentClass => studentClass.Class.TeacherId == teacher.Id);
        }
        else if (User.IsInRole(ApplicationRoles.Student))
        {
            var student = await userProfileService.GetStudentAsync(User, cancellationToken);
            if (student is null)
            {
                return Forbid();
            }

            BoardDescription = "View your scores across enrolled classes.";
            accessibleClasses = accessibleClasses.Where(courseClass =>
                courseClass.StudentClasses.Any(studentClass => studentClass.StudentId == student.Id));
            accessibleStudentClasses = accessibleStudentClasses.Where(studentClass => studentClass.StudentId == student.Id);
        }
        else
        {
            BoardDescription = "Review scoreboards for every student and class.";
        }

        ClassOptions = await accessibleClasses
            .OrderBy(courseClass => courseClass.ClassName)
            .Select(courseClass => new SelectListItem(courseClass.ClassName, courseClass.Id.ToString()))
            .ToListAsync(cancellationToken);

        if (ClassId.HasValue)
        {
            accessibleStudentClasses = accessibleStudentClasses.Where(studentClass => studentClass.ClassId == ClassId.Value);
        }

        var roster = await accessibleStudentClasses
            .OrderBy(studentClass => studentClass.Class.ClassName)
            .ThenBy(studentClass => studentClass.Student.FullName)
            .Select(studentClass => new
            {
                studentClass.ClassId,
                ClassName = studentClass.Class.ClassName,
                studentClass.StudentId,
                StudentName = studentClass.Student.FullName
            })
            .ToListAsync(cancellationToken);

        var classIds = roster.Select(item => item.ClassId).Distinct().ToList();
        var studentIds = roster.Select(item => item.StudentId).Distinct().ToList();

        var scores = classIds.Count == 0 || studentIds.Count == 0
            ? []
            : await context.Scores
                .AsNoTracking()
                .Where(score => classIds.Contains(score.ClassId) && studentIds.Contains(score.StudentId))
                .Select(score => new
                {
                    score.ClassId,
                    score.StudentId,
                    ScoreTypeName = score.ScoreType.Name,
                    score.Value
                })
                .ToListAsync(cancellationToken);

        Rows = roster
            .Select(item =>
            {
                var relatedScores = scores.Where(score => score.ClassId == item.ClassId && score.StudentId == item.StudentId).ToList();
                return new ScoreboardRow
                {
                    ClassId = item.ClassId,
                    ClassName = item.ClassName,
                    StudentId = item.StudentId,
                    StudentName = item.StudentName,
                    Homework = relatedScores.FirstOrDefault(score => score.ScoreTypeName == "Homework")?.Value,
                    Quiz = relatedScores.FirstOrDefault(score => score.ScoreTypeName == "Quiz")?.Value,
                    Midterm = relatedScores.FirstOrDefault(score => score.ScoreTypeName == "Midterm")?.Value,
                    Final = relatedScores.FirstOrDefault(score => score.ScoreTypeName == "Final")?.Value
                };
            })
            .ToList();

        return Page();
    }
}
