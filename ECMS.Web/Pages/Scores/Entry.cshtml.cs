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

namespace ECMS.Web.Pages.Scores;

[Authorize(Roles = ApplicationRoles.Teacher)]
public class EntryModel(
    ApplicationDbContext context,
    UserProfileService userProfileService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? ClassId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ScoreTypeId { get; set; }

    [BindProperty]
    public List<ScoreEntryRow> Entries { get; set; } = [];

    public List<SelectListItem> ClassOptions { get; private set; } = [];

    public List<SelectListItem> ScoreTypeOptions { get; private set; } = [];

    public string? SelectedClassName { get; private set; }

    public string? SelectedScoreTypeName { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var teacher = await userProfileService.GetTeacherAsync(User, cancellationToken);
        if (teacher is null)
        {
            return Forbid();
        }

        await LoadFiltersAsync(teacher.Id, cancellationToken);

        if (ClassId.HasValue && ScoreTypeId.HasValue)
        {
            await LoadEntriesAsync(teacher.Id, cancellationToken);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var teacher = await userProfileService.GetTeacherAsync(User, cancellationToken);
        if (teacher is null)
        {
            return Forbid();
        }

        await LoadFiltersAsync(teacher.Id, cancellationToken);

        if (!ClassId.HasValue || !ScoreTypeId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Please choose a class and score type.");
            return Page();
        }

        var targetClass = await context.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(courseClass => courseClass.Id == ClassId.Value && courseClass.TeacherId == teacher.Id, cancellationToken);

        if (targetClass is null)
        {
            return Forbid();
        }

        foreach (var entry in Entries)
        {
            if (entry.Value is < 0 or > 10)
            {
                ModelState.AddModelError(string.Empty, $"Score for {entry.StudentName} must be between 0 and 10.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadEntriesAsync(teacher.Id, cancellationToken, usePostedValues: true);
            return Page();
        }

        var existingScores = await context.Scores
            .Where(score => score.ClassId == ClassId.Value && score.ScoreTypeId == ScoreTypeId.Value)
            .ToListAsync(cancellationToken);

        foreach (var entry in Entries)
        {
            var existing = existingScores.FirstOrDefault(score => score.StudentId == entry.StudentId);

            if (!entry.Value.HasValue)
            {
                if (existing is not null)
                {
                    context.Scores.Remove(existing);
                }

                continue;
            }

            if (existing is null)
            {
                context.Scores.Add(new Score
                {
                    ClassId = ClassId.Value,
                    ScoreTypeId = ScoreTypeId.Value,
                    StudentId = entry.StudentId,
                    TeacherId = teacher.Id,
                    Value = entry.Value.Value,
                    Notes = entry.Notes?.Trim(),
                    RecordedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                existing.Value = entry.Value.Value;
                existing.Notes = entry.Notes?.Trim();
                existing.TeacherId = teacher.Id;
                existing.RecordedAtUtc = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return RedirectToPage(new { ClassId, ScoreTypeId });
    }

    private async Task LoadFiltersAsync(int teacherId, CancellationToken cancellationToken)
    {
        ClassOptions = await context.Classes
            .AsNoTracking()
            .Where(courseClass => courseClass.TeacherId == teacherId)
            .OrderBy(courseClass => courseClass.ClassName)
            .Select(courseClass => new SelectListItem(courseClass.ClassName, courseClass.Id.ToString()))
            .ToListAsync(cancellationToken);

        ScoreTypeOptions = await context.ScoreTypes
            .AsNoTracking()
            .Where(scoreType => scoreType.IsActive)
            .OrderBy(scoreType => scoreType.Name)
            .Select(scoreType => new SelectListItem(scoreType.Name, scoreType.Id.ToString()))
            .ToListAsync(cancellationToken);
    }

    private async Task LoadEntriesAsync(int teacherId, CancellationToken cancellationToken, bool usePostedValues = false)
    {
        var targetClass = await context.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(courseClass => courseClass.Id == ClassId && courseClass.TeacherId == teacherId, cancellationToken);

        var scoreType = await context.ScoreTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == ScoreTypeId, cancellationToken);

        if (targetClass is null || scoreType is null)
        {
            Entries = [];
            SelectedClassName = null;
            SelectedScoreTypeName = null;
            return;
        }

        SelectedClassName = targetClass.ClassName;
        SelectedScoreTypeName = scoreType.Name;

        if (usePostedValues)
        {
            return;
        }

        var students = await context.StudentClasses
            .AsNoTracking()
            .Where(studentClass => studentClass.ClassId == targetClass.Id)
            .OrderBy(studentClass => studentClass.Student.FullName)
            .Select(studentClass => new
            {
                studentClass.StudentId,
                studentClass.Student.StudentCode,
                studentClass.Student.FullName
            })
            .ToListAsync(cancellationToken);

        var scoreLookup = await context.Scores
            .AsNoTracking()
            .Where(score => score.ClassId == targetClass.Id && score.ScoreTypeId == scoreType.Id)
            .ToDictionaryAsync(score => score.StudentId, cancellationToken);

        Entries = students
            .Select(student => new ScoreEntryRow
            {
                StudentId = student.StudentId,
                StudentCode = student.StudentCode,
                StudentName = student.FullName,
                Value = scoreLookup.TryGetValue(student.StudentId, out var score) ? score.Value : null,
                Notes = scoreLookup.TryGetValue(student.StudentId, out score) ? score.Notes : null
            })
            .ToList();
    }
}
