using Requests.Application.Common;
using Requests.Application.Users;
using Requests.Domain.Entities;

namespace Requests.Application.Requests;

public interface IRequestRepository
{
    Task<PagedResult<Request>> SearchAsync(
        SearchRequestsQuery query,
        CurrentUser currentUser,
        CancellationToken cancellationToken = default);
}
