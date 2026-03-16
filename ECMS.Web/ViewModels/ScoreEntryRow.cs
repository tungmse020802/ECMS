namespace ECMS.Web.ViewModels;

public class ScoreEntryRow
{
    public int StudentId { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public decimal? Value { get; set; }

    public string? Notes { get; set; }
}
