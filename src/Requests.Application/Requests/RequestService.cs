using Requests.Application.Common;
using Requests.Application.Users;
using Requests.Domain.Entities;

namespace Requests.Application.Requests;

public class RequestService : IRequestService
{
    private readonly IRequestRepository _repository;

    public RequestService(IRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<RequestDto>> SearchRequestsAsync(
        SearchRequestsQuery query,
        CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        var result = await _repository.SearchAsync(query, currentUser, cancellationToken);

        var items = result.Items.Select(ToDto).ToList();
        return new PagedResult<RequestDto>(items, result.TotalCount, result.Page, result.PageSize);
    }

    private static RequestDto ToDto(Request request)
        => new(
            request.Id,
            request.RequestNumber,
            request.CustomerId,
            request.OwnerId,
            request.AssignedToUserId,
            request.Status,
            request.RequestType,
            request.CreatedAt);
}
