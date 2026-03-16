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
public class EditModel(ApplicationDbContext context) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public ClassFormModel Input { get; set; } = new();

    public IEnumerable<SelectListItem> TeacherOptions { get; private set; } = [];

    public IEnumerable<SelectListItem> LevelOptions => Enum.GetValues<EnglishLevel>()
        .Select(level => new SelectListItem(level.ToString(), level.ToString()));

    public IEnumerable<SelectListItem> StatusOptions => Enum.GetValues<ClassStatus>()
        .Select(status => new SelectListItem(status.ToString(), status.ToString()));

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var courseClass = await context.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == Id, cancellationToken);

        if (courseClass is null)
        {
            return NotFound();
        }

        Input = new ClassFormModel
        {
            ClassName = courseClass.ClassName,
            Level = courseClass.Level,
            TeacherId = courseClass.TeacherId,
            MaxStudents = courseClass.MaxStudents,
            Status = courseClass.Status
        };

        await LoadTeacherOptionsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var courseClass = await context.Classes.FirstOrDefaultAsync(item => item.Id == Id, cancellationToken);
        if (courseClass is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadTeacherOptionsAsync(cancellationToken);
            return Page();
        }

        courseClass.ClassName = Input.ClassName.Trim();
        courseClass.Level = Input.Level;
        courseClass.TeacherId = Input.TeacherId;
        courseClass.MaxStudents = Input.MaxStudents;
        courseClass.Status = Input.Status;

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
