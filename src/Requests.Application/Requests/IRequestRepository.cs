using Requests.Application.Common;
using Requests.Application.Users;

namespace Requests.Application.Requests;

public interface IRequestRepository
{
    Task<PagedResult<RequestDto>> SearchAsync(
        SearchRequestsQuery query,
        CurrentUser currentUser,
        CancellationToken cancellationToken = default);
}
