using Microsoft.AspNetCore.Identity;

namespace ECMS.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public Student? StudentProfile { get; set; }

    public Teacher? TeacherProfile { get; set; }
}
