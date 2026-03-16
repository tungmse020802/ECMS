using System.ComponentModel.DataAnnotations;

namespace ECMS.Web.Models;

public class Room
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string RoomName { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int Capacity { get; set; } = 30;

    public bool IsActive { get; set; } = true;

    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
