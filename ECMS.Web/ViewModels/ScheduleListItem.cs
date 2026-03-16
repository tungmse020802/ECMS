using ECMS.Web.Models;

namespace ECMS.Web.ViewModels;

public class ScheduleListItem
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public string ClassName { get; set; } = string.Empty;

    public DateTime ClassDate { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public string TeacherName { get; set; } = string.Empty;

    public ScheduleStatus Status { get; set; }
}
