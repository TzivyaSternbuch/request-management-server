using Requests.Application.Common;
using Requests.Application.Users;

namespace Requests.Application.Requests;

public class RequestService : IRequestService
{
    private readonly IRequestRepository _repository;

    public RequestService(IRequestRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<RequestDto>> SearchRequestsAsync(
        SearchRequestsQuery query,
        CurrentUser currentUser,
        CancellationToken cancellationToken = default)
        => _repository.SearchAsync(query, currentUser, cancellationToken);
}
