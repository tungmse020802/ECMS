using System.ComponentModel.DataAnnotations;

namespace ECMS.Web.Models;

public class Score
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public int ClassId { get; set; }

    public CourseClass Class { get; set; } = null!;

    public int ScoreTypeId { get; set; }

    public ScoreType ScoreType { get; set; } = null!;

    [Range(0, 10)]
    public decimal Value { get; set; }

    [StringLength(200)]
    public string? Notes { get; set; }

    public int TeacherId { get; set; }

    public Teacher Teacher { get; set; } = null!;

    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
