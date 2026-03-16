namespace ECMS.Web.ViewModels;

public class ScoreboardRow
{
    public int ClassId { get; set; }

    public string ClassName { get; set; } = string.Empty;

    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public decimal? Homework { get; set; }

    public decimal? Quiz { get; set; }

    public decimal? Midterm { get; set; }

    public decimal? Final { get; set; }

    public decimal? Average
    {
        get
        {
            var values = new decimal?[] { Homework, Quiz, Midterm, Final }
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToList();

            return values.Count == 0
                ? null
                : Math.Round(values.Average(), 2);
        }
    }
}
