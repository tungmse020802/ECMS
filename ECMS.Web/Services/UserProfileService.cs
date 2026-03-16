using System.Security.Claims;
using ECMS.Web.Data;
using ECMS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Services;

public class UserProfileService(ApplicationDbContext context)
{
    public string? GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public async Task<Teacher?> GetTeacherAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(user);
        return string.IsNullOrWhiteSpace(userId)
            ? null
            : await context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(teacher => teacher.ApplicationUserId == userId, cancellationToken);
    }

    public async Task<Student?> GetStudentAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(user);
        return string.IsNullOrWhiteSpace(userId)
            ? null
            : await context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(student => student.ApplicationUserId == userId, cancellationToken);
    }
}
