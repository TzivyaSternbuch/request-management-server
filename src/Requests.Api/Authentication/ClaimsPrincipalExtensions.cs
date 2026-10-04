using System.Globalization;
using System.Security.Claims;
using Requests.Application.Users;

namespace Requests.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal user)
    {
        var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);
        var isAdministrator = user.IsInRole(HeaderUserAuthenticationHandler.AdministratorRole);

        return new CurrentUser(userId, isAdministrator);
    }
}
