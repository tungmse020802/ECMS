using ECMS.Web.Authorization;
using ECMS.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Pages.Students;

[Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.Staff)]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public List<StudentListItem> Students { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Students = await context.Students
            .AsNoTracking()
            .Include(student => student.ApplicationUser)
            .Include(student => student.StudentClasses)
            .OrderBy(student => student.FullName)
            .Select(student => new StudentListItem
            {
                Id = student.Id,
                StudentCode = student.StudentCode,
                FullName = student.FullName,
                Email = student.Email,
                PortalUserName = student.ApplicationUser != null
                    ? student.ApplicationUser.UserName
                    : null,
                EnrollmentCount = student.StudentClasses.Count
            })
            .ToListAsync(cancellationToken);
    }

    public class StudentListItem
    {
        public int Id { get; set; }

        public string StudentCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PortalUserName { get; set; }

        public int EnrollmentCount { get; set; }
    }
}
