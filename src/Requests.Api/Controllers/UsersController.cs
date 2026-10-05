using Microsoft.AspNetCore.Mvc;
using Requests.Api.Authentication;
using Requests.Application.Users;

namespace Requests.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // Authentication has already loaded the user from the database, so the front can use this
    // to log in: 401 for an unknown user, otherwise the user's id and role as the server sees them.
    [HttpGet("me")]
    public ActionResult<CurrentUser> GetCurrentUser()
    {
        return Ok(User.ToCurrentUser());
    }
}
