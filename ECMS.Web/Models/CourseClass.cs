using System.ComponentModel.DataAnnotations;

namespace ECMS.Web.Models;

public class CourseClass
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string ClassName { get; set; } = string.Empty;

    public EnglishLevel Level { get; set; }

    public int? TeacherId { get; set; }

    public Teacher? Teacher { get; set; }

    [Range(1, 1000)]
    public int MaxStudents { get; set; } = 20;

    public ClassStatus Status { get; set; } = ClassStatus.Planned;

    public ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();

    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public ICollection<Score> Scores { get; set; } = new List<Score>();
}
