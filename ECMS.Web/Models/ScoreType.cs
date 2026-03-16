using System.ComponentModel.DataAnnotations;

namespace ECMS.Web.Models;

public class ScoreType
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Score> Scores { get; set; } = new List<Score>();
}
