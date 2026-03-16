using System.ComponentModel.DataAnnotations;

namespace ECMS.Web.Models;

public class Teacher
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(450)]
    public string? ApplicationUserId { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }

    public ICollection<CourseClass> Classes { get; set; } = new List<CourseClass>();

    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public ICollection<Score> ScoresRecorded { get; set; } = new List<Score>();
}
