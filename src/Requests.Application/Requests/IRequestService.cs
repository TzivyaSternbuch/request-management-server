using Requests.Application.Common;
using Requests.Application.Users;

namespace Requests.Application.Requests;

public interface IRequestService
{
    Task<PagedResult<RequestDto>> SearchRequestsAsync(
        SearchRequestsQuery query,
        CurrentUser currentUser,
        CancellationToken cancellationToken = default);
}
