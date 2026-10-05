using Requests.Application.Common;
using Requests.Application.Requests;
using Requests.Application.Users;
using Requests.Domain.Entities;
using Xunit;

namespace Requests.Tests;

public class RequestServiceTests
{
    [Fact]
    public async Task SearchRequests_ReturnsRepositoryResult()
    {
        var createdAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var request = new RequestDto(7, "REQ-000007", 8, 3, null, RequestStatus.New, RequestType.Legal, createdAt);
        var expected = new PagedResult<RequestDto>([request], TotalCount: 42, Page: 2, PageSize: 20);
        var service = new RequestService(new FakeRequestRepository(expected));

        var result = await service.SearchRequestsAsync(new SearchRequestsQuery(), new CurrentUser(1, IsAdministrator: true));

        Assert.Equal([request], result.Items);
        Assert.Equal(42, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    private class FakeRequestRepository : IRequestRepository
    {
        private readonly PagedResult<RequestDto> _result;

        public FakeRequestRepository(PagedResult<RequestDto> result)
        {
            _result = result;
        }

        public Task<PagedResult<RequestDto>> SearchAsync(
            SearchRequestsQuery query,
            CurrentUser currentUser,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }
}
