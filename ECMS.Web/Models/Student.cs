using System.ComponentModel.DataAnnotations;

namespace ECMS.Web.Models;

public class Student
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string StudentCode { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(450)]
    public string? ApplicationUserId { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }

    public ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public ICollection<Score> Scores { get; set; } = new List<Score>();
}
