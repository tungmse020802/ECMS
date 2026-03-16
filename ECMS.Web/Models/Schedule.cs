using System.ComponentModel.DataAnnotations;

namespace ECMS.Web.Models;

public class Schedule
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public CourseClass Class { get; set; } = null!;

    [DataType(DataType.Date)]
    public DateTime ClassDate { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public int RoomId { get; set; }

    public Room Room { get; set; } = null!;

    public int TeacherId { get; set; }

    public Teacher Teacher { get; set; } = null!;

    public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
