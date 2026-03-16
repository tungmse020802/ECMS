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
public class EditModel(
    ApplicationDbContext context,
    ProfileAccountLookupService profileAccountLookupService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public StudentFormModel Input { get; set; } = new();

    public IEnumerable<SelectListItem> AccountOptions { get; private set; } = [];

    public List<ClassOptionItem> ClassOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var student = await context.Students
            .AsNoTracking()
            .Include(item => item.StudentClasses)
            .FirstOrDefaultAsync(item => item.Id == Id, cancellationToken);

        if (student is null)
        {
            return NotFound();
        }

        Input = new StudentFormModel
        {
            StudentCode = student.StudentCode,
            FullName = student.FullName,
            Email = student.Email,
            ApplicationUserId = student.ApplicationUserId,
            SelectedClassIds = student.StudentClasses.Select(studentClass => studentClass.ClassId).ToList()
        };

        await LoadOptionsAsync(student.Id, student.ApplicationUserId, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var student = await context.Students
            .Include(item => item.StudentClasses)
            .FirstOrDefaultAsync(item => item.Id == Id, cancellationToken);

        if (student is null)
        {
            return NotFound();
        }

        Input.SelectedClassIds = Input.SelectedClassIds.Distinct().ToList();

        await ValidateInputAsync(student.Id, student.ApplicationUserId, cancellationToken);

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(student.Id, student.ApplicationUserId, cancellationToken);
            return Page();
        }

        student.StudentCode = Input.StudentCode.Trim();
        student.FullName = Input.FullName.Trim();
        student.Email = Normalize(Input.Email);
        student.ApplicationUserId = Normalize(Input.ApplicationUserId);

        var selectedClassIds = Input.SelectedClassIds.ToHashSet();
        var existingClassIds = student.StudentClasses.Select(studentClass => studentClass.ClassId).ToHashSet();

        var studentClassesToRemove = student.StudentClasses
            .Where(studentClass => !selectedClassIds.Contains(studentClass.ClassId))
            .ToList();

        context.StudentClasses.RemoveRange(studentClassesToRemove);

        var studentClassesToAdd = selectedClassIds
            .Except(existingClassIds)
            .Select(classId => new StudentClass
            {
                StudentId = student.Id,
                ClassId = classId,
                EnrolledAtUtc = DateTime.UtcNow
            });

        context.StudentClasses.AddRange(studentClassesToAdd);

        await context.SaveChangesAsync(cancellationToken);
        return RedirectToPage("/Students/Details", new { id = student.Id });
    }

    private async Task ValidateInputAsync(
        int currentStudentId,
        string? currentLinkedUserId,
        CancellationToken cancellationToken)
    {
        var normalizedStudentCode = Input.StudentCode.Trim();

        if (await context.Students.AnyAsync(
                student => student.StudentCode == normalizedStudentCode && student.Id != currentStudentId,
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
                StudentCount = courseClass.StudentClasses.Count(studentClass => studentClass.StudentId != currentStudentId)
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

    private async Task LoadOptionsAsync(
        int currentStudentId,
        string? currentLinkedUserId,
        CancellationToken cancellationToken)
    {
        AccountOptions = await profileAccountLookupService.GetStudentAccountOptionsAsync(
            currentLinkedUserId,
            cancellationToken);

        await LoadClassOptionsAsync(currentStudentId, cancellationToken);
    }

    private async Task LoadClassOptionsAsync(int currentStudentId, CancellationToken cancellationToken)
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
                StudentCount = courseClass.StudentClasses.Count(studentClass => studentClass.StudentId != currentStudentId),
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
