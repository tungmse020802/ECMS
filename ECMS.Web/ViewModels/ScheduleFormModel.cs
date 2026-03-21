using System.ComponentModel.DataAnnotations;
using ECMS.Web.Models;

namespace ECMS.Web.ViewModels;

public class ScheduleFormModel
{
    [Required]
    [Display(Name = "Class")]
    public int ClassId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime ClassDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "Start time")]
    public TimeSpan StartTime { get; set; } = new(8, 0, 0);

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "End time")]
    public TimeSpan EndTime { get; set; } = new(10, 0, 0);

    [Required]
    [Display(Name = "Room")]
    public int RoomId { get; set; }

    [Required]
    [Display(Name = "Teacher")]
    public int TeacherId { get; set; }

    [Display(Name = "Status")]
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;

    [Required]
    public string TimeZoneId { get; set; } = string.Empty;
}
