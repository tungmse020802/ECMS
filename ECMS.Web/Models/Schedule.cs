using System.ComponentModel.DataAnnotations;

namespace ECMS.Web.Models;

public class Schedule
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public CourseClass Class { get; set; } = null!;

    [DataType(DataType.DateTime)]
    public DateTime StartAtUtc { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime EndAtUtc { get; set; }

    public int RoomId { get; set; }

    public Room Room { get; set; } = null!;

    public int TeacherId { get; set; }

    public Teacher Teacher { get; set; } = null!;

    public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
