using Requests.Application.Common;
using Requests.Application.Requests;
using Requests.Application.Users;
using Requests.Domain.Entities;
using Xunit;

namespace Requests.Tests;

public class RequestServiceTests
{
    [Fact]
    public async Task SearchRequests_MapsItemsToDtosAndKeepsPagingInfo()
    {
        var createdAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var request = new Request
        {
            Id = 7,
            RequestNumber = "REQ-000007",
            CustomerId = 8,
            OwnerId = 3,
            AssignedToUserId = null,
            Status = RequestStatus.New,
            RequestType = RequestType.Legal,
            CreatedAt = createdAt
        };
        var repository = new FakeRequestRepository(
            new PagedResult<Request>([request], TotalCount: 42, Page: 2, PageSize: 20));

        var service = new RequestService(repository);

        var result = await service.SearchRequestsAsync(new SearchRequestsQuery(), new CurrentUser(1, IsAdministrator: true));

        var expected = new RequestDto(7, "REQ-000007", 8, 3, null, RequestStatus.New, RequestType.Legal, createdAt);
        Assert.Equal([expected], result.Items);
        Assert.Equal(42, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    private class FakeRequestRepository : IRequestRepository
    {
        private readonly PagedResult<Request> _result;

        public FakeRequestRepository(PagedResult<Request> result)
        {
            _result = result;
        }

        public Task<PagedResult<Request>> SearchAsync(
            SearchRequestsQuery query,
            CurrentUser currentUser,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }
}
