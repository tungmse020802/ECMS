using ECMS.Web.Authorization;
using ECMS.Web.Data;
using ECMS.Web.Models;
using ECMS.Web.Services;
using ECMS.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECMS.Web.Pages.Teachers;

[Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.Staff)]
public class CreateModel(
    ApplicationDbContext context,
    ProfileAccountLookupService profileAccountLookupService) : PageModel
{
    [BindProperty]
    public TeacherFormModel Input { get; set; } = new();

    public IEnumerable<SelectListItem> AccountOptions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAccountOptionsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await IsValidAccountSelectionAsync(cancellationToken))
        {
            ModelState.AddModelError(nameof(Input.ApplicationUserId), "The selected portal account is unavailable.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAccountOptionsAsync(cancellationToken);
            return Page();
        }

        var teacher = new Teacher
        {
            FullName = Input.FullName.Trim(),
            Email = Normalize(Input.Email),
            ApplicationUserId = Normalize(Input.ApplicationUserId)
        };

        context.Teachers.Add(teacher);
        await context.SaveChangesAsync(cancellationToken);

        return RedirectToPage("/Teachers/Details", new { id = teacher.Id });
    }

    private async Task LoadAccountOptionsAsync(CancellationToken cancellationToken)
    {
        AccountOptions = await profileAccountLookupService.GetTeacherAccountOptionsAsync(
            cancellationToken: cancellationToken);
    }

    private async Task<bool> IsValidAccountSelectionAsync(CancellationToken cancellationToken)
    {
        var selectedUserId = Normalize(Input.ApplicationUserId);
        if (selectedUserId is null)
        {
            return true;
        }

        return (await profileAccountLookupService.GetTeacherAccountOptionsAsync(
                cancellationToken: cancellationToken))
            .Any(option => option.Value == selectedUserId);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
