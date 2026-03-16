using ECMS.Web.Data;
using ECMS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Services;

public class ScheduleConflictService(ApplicationDbContext context)
{
    public async Task<IReadOnlyList<string>> ValidateAsync(
        int classId,
        DateTime classDate,
        TimeSpan startTime,
        TimeSpan endTime,
        int roomId,
        int teacherId,
        int? scheduleId = null,
        CancellationToken cancellationToken = default)
    {
        List<string> errors = [];

        if (startTime >= endTime)
        {
            errors.Add("Start time must be earlier than end time.");
            return errors;
        }

        var schedules = await context.Schedules
            .Where(schedule =>
                schedule.Status == ScheduleStatus.Scheduled &&
                schedule.ClassDate.Date == classDate.Date &&
                (!scheduleId.HasValue || schedule.Id != scheduleId.Value) &&
                startTime < schedule.EndTime &&
                endTime > schedule.StartTime)
            .Select(schedule => new
            {
                schedule.RoomId,
                schedule.TeacherId
            })
            .ToListAsync(cancellationToken);

        if (schedules.Any(schedule => schedule.RoomId == roomId))
        {
            errors.Add("The selected room is already booked for this time slot.");
        }

        if (schedules.Any(schedule => schedule.TeacherId == teacherId))
        {
            errors.Add("The selected teacher already has a teaching slot at this time.");
        }

        var classExists = await context.Classes.AnyAsync(courseClass => courseClass.Id == classId, cancellationToken);
        if (!classExists)
        {
            errors.Add("The selected class does not exist.");
        }

        return errors;
    }
}
