using System.ComponentModel.DataAnnotations;

namespace ECMS.Web.ViewModels;

public class TeacherFormModel
{
    [Required]
    [StringLength(150)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(150)]
    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Portal account")]
    public string? ApplicationUserId { get; set; }
}
