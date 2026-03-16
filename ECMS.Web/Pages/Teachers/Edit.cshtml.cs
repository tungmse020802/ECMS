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

namespace ECMS.Web.Pages.Teachers;

[Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.Staff)]
public class EditModel(
    ApplicationDbContext context,
    ProfileAccountLookupService profileAccountLookupService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public TeacherFormModel Input { get; set; } = new();

    public IEnumerable<SelectListItem> AccountOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var teacher = await context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == Id, cancellationToken);

        if (teacher is null)
        {
            return NotFound();
        }

        Input = new TeacherFormModel
        {
            FullName = teacher.FullName,
            Email = teacher.Email,
            ApplicationUserId = teacher.ApplicationUserId
        };

        await LoadAccountOptionsAsync(teacher.ApplicationUserId, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var teacher = await context.Teachers.FirstOrDefaultAsync(item => item.Id == Id, cancellationToken);
        if (teacher is null)
        {
            return NotFound();
        }

        if (!await IsValidAccountSelectionAsync(teacher.ApplicationUserId, cancellationToken))
        {
            ModelState.AddModelError(nameof(Input.ApplicationUserId), "The selected portal account is unavailable.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAccountOptionsAsync(teacher.ApplicationUserId, cancellationToken);
            return Page();
        }

        teacher.FullName = Input.FullName.Trim();
        teacher.Email = Normalize(Input.Email);
        teacher.ApplicationUserId = Normalize(Input.ApplicationUserId);

        await context.SaveChangesAsync(cancellationToken);
        return RedirectToPage("/Teachers/Details", new { id = teacher.Id });
    }

    private async Task LoadAccountOptionsAsync(string? currentLinkedUserId, CancellationToken cancellationToken)
    {
        AccountOptions = await profileAccountLookupService.GetTeacherAccountOptionsAsync(
            currentLinkedUserId,
            cancellationToken);
    }

    private async Task<bool> IsValidAccountSelectionAsync(string? currentLinkedUserId, CancellationToken cancellationToken)
    {
        var selectedUserId = Normalize(Input.ApplicationUserId);
        if (selectedUserId is null)
        {
            return true;
        }

        return (await profileAccountLookupService.GetTeacherAccountOptionsAsync(
                currentLinkedUserId,
                cancellationToken))
            .Any(option => option.Value == selectedUserId);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
