using ECMS.Web.Authorization;
using ECMS.Web.Data;
using ECMS.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Pages.Teachers;

[Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.Staff)]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public List<TeacherListItem> Teachers { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        Teachers = await context.Teachers
            .AsNoTracking()
            .Include(teacher => teacher.ApplicationUser)
            .Include(teacher => teacher.Classes)
            .Include(teacher => teacher.Schedules)
            .OrderBy(teacher => teacher.FullName)
            .Select(teacher => new TeacherListItem
            {
                Id = teacher.Id,
                FullName = teacher.FullName,
                Email = teacher.Email,
                PortalUserName = teacher.ApplicationUser != null
                    ? teacher.ApplicationUser.UserName
                    : null,
                ClassCount = teacher.Classes.Count,
                UpcomingSessionCount = teacher.Schedules.Count(schedule =>
                    schedule.Status == ScheduleStatus.Scheduled &&
                    schedule.ClassDate >= today)
            })
            .ToListAsync(cancellationToken);
    }

    public class TeacherListItem
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PortalUserName { get; set; }

        public int ClassCount { get; set; }

        public int UpcomingSessionCount { get; set; }
    }
}
