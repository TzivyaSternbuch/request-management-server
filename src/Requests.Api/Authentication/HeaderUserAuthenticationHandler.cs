using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Requests.Api.Authentication;

// For the exercise, the current user is supplied through headers:
// X-User-Id: positive integer (required)
// X-Is-Admin: true|false (optional, anything other than "true" means not an administrator)
public class HeaderUserAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "HeaderUser";
    public const string UserIdHeader = "X-User-Id";
    public const string IsAdminHeader = "X-Is-Admin";
    public const string AdministratorRole = "Administrator";

    private const string InvalidUserMessage = $"Header {UserIdHeader} is missing or not a positive integer.";

    public HeaderUserAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userIdValue = Request.Headers[UserIdHeader].FirstOrDefault();
        if (!int.TryParse(userIdValue, out var userId) || userId <= 0)
            return Task.FromResult(AuthenticateResult.Fail(InvalidUserMessage));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture))
        };

        var isAdministrator = string.Equals(
            Request.Headers[IsAdminHeader].FirstOrDefault(),
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (isAdministrator)
            claims.Add(new Claim(ClaimTypes.Role, AdministratorRole));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        return Results
            .Problem(detail: InvalidUserMessage, statusCode: StatusCodes.Status401Unauthorized)
            .ExecuteAsync(Context);
    }
}
