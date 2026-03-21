using System.Security.Claims;
using ECMS.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ECMS.Web.Services;

public class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(
            IdentityConstants.ApplicationScheme,
            Options.ClaimsIdentity.UserNameClaimType,
            Options.ClaimsIdentity.RoleClaimType);

        identity.AddClaim(new Claim(Options.ClaimsIdentity.UserIdClaimType, await UserManager.GetUserIdAsync(user)));

        var userName = await UserManager.GetUserNameAsync(user);
        if (!string.IsNullOrWhiteSpace(userName))
        {
            identity.AddClaim(new Claim(Options.ClaimsIdentity.UserNameClaimType, userName));
        }

        if (UserManager.SupportsUserEmail)
        {
            var email = await UserManager.GetEmailAsync(user);
            if (!string.IsNullOrWhiteSpace(email))
            {
                identity.AddClaim(new Claim(Options.ClaimsIdentity.EmailClaimType, email));
            }
        }

        if (UserManager.SupportsUserSecurityStamp)
        {
            var securityStamp = await UserManager.GetSecurityStampAsync(user);
            if (!string.IsNullOrWhiteSpace(securityStamp))
            {
                identity.AddClaim(new Claim(Options.ClaimsIdentity.SecurityStampClaimType, securityStamp));
            }
        }

        if (UserManager.SupportsUserRole)
        {
            var roles = await UserManager.GetRolesAsync(user);

            foreach (var roleName in roles)
            {
                identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, roleName));

                if (!RoleManager.SupportsRoleClaims)
                {
                    continue;
                }

                var role = await RoleManager.FindByNameAsync(roleName);
                if (role is null)
                {
                    continue;
                }

                identity.AddClaims(await RoleManager.GetClaimsAsync(role));
            }
        }

        return identity;
    }
}
