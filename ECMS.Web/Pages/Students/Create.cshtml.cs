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

namespace ECMS.Web.Pages.Students;

[Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.Staff)]
public class CreateModel(
    ApplicationDbContext context,
    ProfileAccountLookupService profileAccountLookupService) : PageModel
{
    [BindProperty]
    public StudentFormModel Input { get; set; } = new();

    public IEnumerable<SelectListItem> AccountOptions { get; private set; } = [];

    public List<ClassOptionItem> ClassOptions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(null, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Input.SelectedClassIds = Input.SelectedClassIds.Distinct().ToList();

        await ValidateInputAsync(null, null, cancellationToken);

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(null, cancellationToken);
            return Page();
        }

        var student = new Student
        {
            StudentCode = Input.StudentCode.Trim(),
            FullName = Input.FullName.Trim(),
            Email = Normalize(Input.Email),
            ApplicationUserId = Normalize(Input.ApplicationUserId)
        };

        context.Students.Add(student);
        await context.SaveChangesAsync(cancellationToken);

        if (Input.SelectedClassIds.Count > 0)
        {
            context.StudentClasses.AddRange(Input.SelectedClassIds.Select(classId => new StudentClass
            {
                StudentId = student.Id,
                ClassId = classId,
                EnrolledAtUtc = DateTime.UtcNow
            }));

            await context.SaveChangesAsync(cancellationToken);
        }

        return RedirectToPage("/Students/Details", new { id = student.Id });
    }

    private async Task ValidateInputAsync(
        int? currentStudentId,
        string? currentLinkedUserId,
        CancellationToken cancellationToken)
    {
        var normalizedStudentCode = Input.StudentCode.Trim();
        var normalizedAccountId = Normalize(Input.ApplicationUserId);

        if (await context.Students.AnyAsync(
                student => student.StudentCode == normalizedStudentCode &&
                           (!currentStudentId.HasValue || student.Id != currentStudentId.Value),
                cancellationToken))
        {
            ModelState.AddModelError(nameof(Input.StudentCode), "Student code must be unique.");
        }

        if (!await IsValidAccountSelectionAsync(currentLinkedUserId, cancellationToken))
        {
            ModelState.AddModelError(nameof(Input.ApplicationUserId), "The selected portal account is unavailable.");
        }

        var selectedClassIds = Input.SelectedClassIds.Distinct().ToList();
        if (selectedClassIds.Count == 0)
        {
            return;
        }

        var selectedClasses = await context.Classes
            .AsNoTracking()
            .Where(courseClass => selectedClassIds.Contains(courseClass.Id))
            .Select(courseClass => new
            {
                courseClass.Id,
                courseClass.ClassName,
                courseClass.MaxStudents,
                StudentCount = courseClass.StudentClasses.Count(studentClass =>
                    !currentStudentId.HasValue || studentClass.StudentId != currentStudentId.Value)
            })
            .ToListAsync(cancellationToken);

        if (selectedClasses.Count != selectedClassIds.Count)
        {
            ModelState.AddModelError(nameof(Input.SelectedClassIds), "One or more selected classes do not exist.");
            return;
        }

        foreach (var courseClass in selectedClasses.Where(courseClass => courseClass.StudentCount >= courseClass.MaxStudents))
        {
            ModelState.AddModelError(
                nameof(Input.SelectedClassIds),
                $"{courseClass.ClassName} has already reached maximum capacity.");
        }
    }

    private async Task LoadOptionsAsync(int? currentStudentId, CancellationToken cancellationToken)
    {
        AccountOptions = await profileAccountLookupService.GetStudentAccountOptionsAsync(
            cancellationToken: cancellationToken);

        await LoadClassOptionsAsync(currentStudentId, cancellationToken);
    }

    private async Task LoadClassOptionsAsync(int? currentStudentId, CancellationToken cancellationToken)
    {
        var selectedClassIds = Input.SelectedClassIds.ToHashSet();

        ClassOptions = await context.Classes
            .AsNoTracking()
            .Include(courseClass => courseClass.Teacher)
            .Include(courseClass => courseClass.StudentClasses)
            .OrderBy(courseClass => courseClass.ClassName)
            .Select(courseClass => new ClassOptionItem
            {
                Id = courseClass.Id,
                ClassName = courseClass.ClassName,
                Level = courseClass.Level,
                TeacherName = courseClass.Teacher != null ? courseClass.Teacher.FullName : "Unassigned",
                StudentCount = courseClass.StudentClasses.Count(studentClass =>
                    !currentStudentId.HasValue || studentClass.StudentId != currentStudentId.Value),
                MaxStudents = courseClass.MaxStudents,
                IsSelected = selectedClassIds.Contains(courseClass.Id)
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> IsValidAccountSelectionAsync(string? currentLinkedUserId, CancellationToken cancellationToken)
    {
        var selectedUserId = Normalize(Input.ApplicationUserId);
        if (selectedUserId is null)
        {
            return true;
        }

        return (await profileAccountLookupService.GetStudentAccountOptionsAsync(
                currentLinkedUserId,
                cancellationToken))
            .Any(option => option.Value == selectedUserId);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public class ClassOptionItem
    {
        public int Id { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public EnglishLevel Level { get; set; }

        public string TeacherName { get; set; } = string.Empty;

        public int StudentCount { get; set; }

        public int MaxStudents { get; set; }

        public bool IsSelected { get; set; }

        public bool IsFull => StudentCount >= MaxStudents;
    }
}
