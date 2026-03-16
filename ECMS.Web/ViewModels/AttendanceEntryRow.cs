using ECMS.Web.Models;

namespace ECMS.Web.ViewModels;

public class AttendanceEntryRow
{
    public int StudentId { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
}
