using ECMS.Web.Authorization;
using ECMS.Web.Data;
using ECMS.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Pages.Classes;

[Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.Staff)]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public List<ClassListItem> Classes { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Classes = await context.Classes
            .AsNoTracking()
            .Include(courseClass => courseClass.Teacher)
            .Include(courseClass => courseClass.StudentClasses)
            .OrderBy(courseClass => courseClass.ClassName)
            .Select(courseClass => new ClassListItem
            {
                Id = courseClass.Id,
                ClassName = courseClass.ClassName,
                Level = courseClass.Level,
                TeacherName = courseClass.Teacher != null ? courseClass.Teacher.FullName : "Unassigned",
                MaxStudents = courseClass.MaxStudents,
                StudentCount = courseClass.StudentClasses.Count,
                Status = courseClass.Status
            })
            .ToListAsync(cancellationToken);
    }

    public class ClassListItem
    {
        public int Id { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public EnglishLevel Level { get; set; }

        public string TeacherName { get; set; } = string.Empty;

        public int MaxStudents { get; set; }

        public int StudentCount { get; set; }

        public ClassStatus Status { get; set; }
    }
}
