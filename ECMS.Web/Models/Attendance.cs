namespace ECMS.Web.Models;

public class Attendance
{
    public int Id { get; set; }

    public int ScheduleId { get; set; }

    public Schedule Schedule { get; set; } = null!;

    public int StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;

    public string RecordedByUserId { get; set; } = string.Empty;
}
