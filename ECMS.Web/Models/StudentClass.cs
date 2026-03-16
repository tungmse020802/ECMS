namespace ECMS.Web.Models;

public class StudentClass
{
    public int StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public int ClassId { get; set; }

    public CourseClass Class { get; set; } = null!;

    public DateTime EnrolledAtUtc { get; set; } = DateTime.UtcNow;
}
