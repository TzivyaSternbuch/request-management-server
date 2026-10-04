using Microsoft.AspNetCore.Mvc;
using Requests.Api.Authentication;
using Requests.Application.Common;
using Requests.Application.Requests;

namespace Requests.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RequestsController : ControllerBase
{
    private readonly IRequestService _service;

    public RequestsController(IRequestService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<RequestDto>>> Search(
        [FromQuery] SearchRequestsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _service.SearchRequestsAsync(query, User.ToCurrentUser(), cancellationToken);
        return Ok(result);
    }
}
