using System.ComponentModel.DataAnnotations;
using ECMS.Web.Models;

namespace ECMS.Web.ViewModels;

public class ClassFormModel
{
    [Required]
    [StringLength(120)]
    [Display(Name = "Class name")]
    public string ClassName { get; set; } = string.Empty;

    [Display(Name = "Level")]
    public EnglishLevel Level { get; set; } = EnglishLevel.Starter;

    [Display(Name = "Teacher")]
    public int? TeacherId { get; set; }

    [Range(1, 1000)]
    [Display(Name = "Maximum students")]
    public int MaxStudents { get; set; } = 20;

    [Display(Name = "Status")]
    public ClassStatus Status { get; set; } = ClassStatus.Planned;
}
