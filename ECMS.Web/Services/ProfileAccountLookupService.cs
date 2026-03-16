using ECMS.Web.Authorization;
using ECMS.Web.Data;
using ECMS.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ECMS.Web.Services;

public class ProfileAccountLookupService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager)
{
    public Task<List<SelectListItem>> GetTeacherAccountOptionsAsync(
        string? currentLinkedUserId = null,
        CancellationToken cancellationToken = default)
    {
        return GetRoleAccountOptionsAsync(
            ApplicationRoles.Teacher,
            context.Teachers
                .AsNoTracking()
                .Where(teacher => teacher.ApplicationUserId != null)
                .Select(teacher => teacher.ApplicationUserId!),
            currentLinkedUserId,
            cancellationToken);
    }

    public Task<List<SelectListItem>> GetStudentAccountOptionsAsync(
        string? currentLinkedUserId = null,
        CancellationToken cancellationToken = default)
    {
        return GetRoleAccountOptionsAsync(
            ApplicationRoles.Student,
            context.Students
                .AsNoTracking()
                .Where(student => student.ApplicationUserId != null)
                .Select(student => student.ApplicationUserId!),
            currentLinkedUserId,
            cancellationToken);
    }

    private async Task<List<SelectListItem>> GetRoleAccountOptionsAsync(
        string roleName,
        IQueryable<string> linkedUserIdsQuery,
        string? currentLinkedUserId,
        CancellationToken cancellationToken)
    {
        var linkedUserIds = (await linkedUserIdsQuery.ToListAsync(cancellationToken))
            .Where(userId => userId != currentLinkedUserId)
            .ToHashSet(StringComparer.Ordinal);

        var roleUsers = await userManager.GetUsersInRoleAsync(roleName);

        return roleUsers
            .Where(user => user.Id == currentLinkedUserId || !linkedUserIds.Contains(user.Id))
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.UserName)
            .Select(user => new SelectListItem(
                $"{user.FullName} ({user.UserName})",
                user.Id))
            .ToList();
    }
}
