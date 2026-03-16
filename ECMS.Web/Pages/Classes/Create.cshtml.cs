using ECMS.Web.Authorization;
using ECMS.Web.Data;
using ECMS.Web.Models;
using ECMS.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Pages.Classes;

[Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.Staff)]
public class CreateModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public ClassFormModel Input { get; set; } = new();

    public IEnumerable<SelectListItem> TeacherOptions { get; private set; } = [];

    public IEnumerable<SelectListItem> LevelOptions => Enum.GetValues<EnglishLevel>()
        .Select(level => new SelectListItem(level.ToString(), level.ToString()));

    public IEnumerable<SelectListItem> StatusOptions => Enum.GetValues<ClassStatus>()
        .Select(status => new SelectListItem(status.ToString(), status.ToString()));

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadTeacherOptionsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadTeacherOptionsAsync(cancellationToken);
            return Page();
        }

        var courseClass = new CourseClass
        {
            ClassName = Input.ClassName.Trim(),
            Level = Input.Level,
            TeacherId = Input.TeacherId,
            MaxStudents = Input.MaxStudents,
            Status = Input.Status
        };

        context.Classes.Add(courseClass);
        await context.SaveChangesAsync(cancellationToken);

        return RedirectToPage("/Classes/Details", new { id = courseClass.Id });
    }

    private async Task LoadTeacherOptionsAsync(CancellationToken cancellationToken)
    {
        TeacherOptions = await context.Teachers
            .AsNoTracking()
            .OrderBy(teacher => teacher.FullName)
            .Select(teacher => new SelectListItem(teacher.FullName, teacher.Id.ToString()))
            .ToListAsync(cancellationToken);
    }
}
