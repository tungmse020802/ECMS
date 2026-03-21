using ECMS.Web.Models;
using ECMS.Web.Options;
using ECMS.Web.ViewModels;
using Microsoft.Extensions.Options;

namespace ECMS.Web.Services;

public class ScheduleDateTimeService(IOptions<SchedulingOptions> options)
{
    public const string TimeZoneCookieName = "ecms_timezone";

    public TimeZoneInfo ResolveTimeZone(string? requestedTimeZoneId = null)
    {
        foreach (var candidate in GetCandidateTimeZoneIds(requestedTimeZoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    public TimeZoneInfo ResolveTimeZone(HttpContext? httpContext, string? requestedTimeZoneId = null)
    {
        if (!string.IsNullOrWhiteSpace(requestedTimeZoneId))
        {
            return ResolveTimeZone(requestedTimeZoneId);
        }

        var cookieTimeZoneId = httpContext?.Request.Cookies[TimeZoneCookieName];
        return ResolveTimeZone(cookieTimeZoneId);
    }

    public DateTime GetLocalNow(TimeZoneInfo timeZone)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
    }

    public DateTime GetLocalToday(TimeZoneInfo timeZone)
    {
        return GetLocalNow(timeZone).Date;
    }

    public DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public bool TryConvertLocalToUtc(
        DateTime localDate,
        TimeSpan localTime,
        TimeZoneInfo timeZone,
        out DateTime utcDateTime,
        out string? errorMessage)
    {
        var localDateTime = DateTime.SpecifyKind(localDate.Date.Add(localTime), DateTimeKind.Unspecified);

        if (timeZone.IsInvalidTime(localDateTime))
        {
            utcDateTime = default;
            errorMessage = "The selected local time does not exist in the selected time zone.";
            return false;
        }

        utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
        errorMessage = null;
        return true;
    }

    public (DateTime LocalStart, DateTime LocalEnd) ConvertUtcToLocalRange(
        DateTime startAtUtc,
        DateTime endAtUtc,
        TimeZoneInfo timeZone)
    {
        return (
            TimeZoneInfo.ConvertTimeFromUtc(NormalizeUtc(startAtUtc), timeZone),
            TimeZoneInfo.ConvertTimeFromUtc(NormalizeUtc(endAtUtc), timeZone));
    }

    public (DateTime StartUtc, DateTime EndUtc) GetUtcRangeForLocalDates(
        DateTime fromDate,
        DateTime toDateInclusive,
        TimeZoneInfo timeZone)
    {
        var startUtc = ConvertBoundaryToUtc(fromDate.Date, timeZone);
        var endUtc = ConvertBoundaryToUtc(toDateInclusive.Date.AddDays(1), timeZone);
        return (startUtc, endUtc);
    }

    public string GetTimeZoneLabel(TimeZoneInfo timeZone)
    {
        var offset = timeZone.BaseUtcOffset;
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var absoluteOffset = offset.Duration();
        return $"{timeZone.Id} (UTC{sign}{absoluteOffset:hh\\:mm})";
    }

    public ScheduleListItem BuildScheduleListItem(Schedule schedule, TimeZoneInfo timeZone, bool canModify)
    {
        var (localStart, localEnd) = ConvertUtcToLocalRange(schedule.StartAtUtc, schedule.EndAtUtc, timeZone);

        return new ScheduleListItem
        {
            Id = schedule.Id,
            ClassId = schedule.ClassId,
            ClassName = schedule.Class.ClassName,
            StartAtUtc = NormalizeUtc(schedule.StartAtUtc),
            EndAtUtc = NormalizeUtc(schedule.EndAtUtc),
            ClassDate = localStart.Date,
            StartTime = localStart.TimeOfDay,
            EndTime = localEnd.TimeOfDay,
            RoomName = schedule.Room.RoomName,
            TeacherName = schedule.Teacher.FullName,
            Status = schedule.Status,
            CanModify = canModify
        };
    }

    private DateTime ConvertBoundaryToUtc(DateTime localBoundary, TimeZoneInfo timeZone)
    {
        var boundary = DateTime.SpecifyKind(localBoundary, DateTimeKind.Unspecified);

        if (timeZone.IsInvalidTime(boundary))
        {
            boundary = boundary.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(boundary, timeZone);
    }

    private IEnumerable<string> GetCandidateTimeZoneIds(string? requestedTimeZoneId)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(requestedTimeZoneId))
        {
            candidates.Add(requestedTimeZoneId);
            candidates.AddRange(GetAliases(requestedTimeZoneId));
        }

        if (!string.IsNullOrWhiteSpace(options.Value.DefaultTimeZoneId))
        {
            candidates.Add(options.Value.DefaultTimeZoneId);
            candidates.AddRange(GetAliases(options.Value.DefaultTimeZoneId));
        }

        candidates.Add(TimeZoneInfo.Utc.Id);
        return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> GetAliases(string timeZoneId)
    {
        return timeZoneId switch
        {
            "Asia/Ho_Chi_Minh" => ["SE Asia Standard Time"],
            "SE Asia Standard Time" => ["Asia/Ho_Chi_Minh"],
            _ => []
        };
    }
}
