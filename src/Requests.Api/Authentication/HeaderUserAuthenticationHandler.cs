using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Requests.Application.Users;

namespace Requests.Api.Authentication;

// For the exercise, the current user is supplied through the X-User-Id header (a positive integer).
// The user must exist, and whether they are an administrator is read from the database,
// never from the request, so a client cannot make itself an administrator.
public class HeaderUserAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "HeaderUser";
    public const string UserIdHeader = "X-User-Id";
    public const string AdministratorRole = "Administrator";

    private const string InvalidUserMessage = $"Header {UserIdHeader} is missing or is not the id of an existing user.";

    private readonly IUserService _userService;

    public HeaderUserAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IUserService userService)
        : base(options, logger, encoder)
    {
        _userService = userService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userIdValue = Request.Headers[UserIdHeader].FirstOrDefault();
        if (!int.TryParse(userIdValue, out var userId) || userId <= 0)
            return AuthenticateResult.Fail(InvalidUserMessage);

        var user = await _userService.FindUserAsync(userId, Context.RequestAborted);
        if (user is null)
            return AuthenticateResult.Fail(InvalidUserMessage);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString(CultureInfo.InvariantCulture))
        };
        if (user.IsAdministrator)
            claims.Add(new Claim(ClaimTypes.Role, AdministratorRole));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        return Results
            .Problem(detail: InvalidUserMessage, statusCode: StatusCodes.Status401Unauthorized)
            .ExecuteAsync(Context);
    }
}
