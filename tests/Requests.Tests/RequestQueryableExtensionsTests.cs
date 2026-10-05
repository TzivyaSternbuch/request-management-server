using Requests.Application.Common;
using Requests.Application.Requests;
using Requests.Application.Users;
using Requests.Domain.Entities;
using Xunit;

namespace Requests.Tests;

public class RequestQueryableExtensionsTests
{
    private static readonly DateTime Day = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void VisibleTo_Administrator_ReturnsAllRequests()
    {
        var requests = new[]
        {
            Create(1, ownerId: 2, assignedTo: 3),
            Create(2, ownerId: 4, assignedTo: null)
        };

        var result = requests.AsQueryable().VisibleTo(new CurrentUser(1, IsAdministrator: true));

        Assert.Equal([1, 2], Ids(result));
    }

    [Fact]
    public void VisibleTo_RegularUser_ReturnsOwnedOrAssignedOnly()
    {
        var requests = new[]
        {
            Create(1, ownerId: 1, assignedTo: 5),
            Create(2, ownerId: 3, assignedTo: 1),
            Create(3, ownerId: 3, assignedTo: 5),
            Create(4, ownerId: 3, assignedTo: null)
        };

        var result = requests.AsQueryable().VisibleTo(new CurrentUser(1, IsAdministrator: false));

        Assert.Equal([1, 2], Ids(result));
    }

    [Fact]
    public void ApplyFilters_RequestNumberPart_ReturnsMatchingRequests()
    {
        var requests = new[] { Create(2), Create(3), Create(20) };

        var result = requests.AsQueryable().ApplyFilters(new SearchRequestsQuery { RequestNumber = "0002" });

        Assert.Equal([2, 20], Ids(result));
    }

    [Fact]
    public void ApplyFilters_SeveralStatuses_ReturnsAnyOfThem()
    {
        var requests = new[]
        {
            Create(1, status: RequestStatus.New),
            Create(2, status: RequestStatus.InProgress),
            Create(3, status: RequestStatus.Cancelled)
        };

        var result = requests.AsQueryable().ApplyFilters(new SearchRequestsQuery
        {
            Status = [RequestStatus.New, RequestStatus.InProgress]
        });

        Assert.Equal([1, 2], Ids(result));
    }

    [Fact]
    public void ApplyFilters_SeveralTypes_ReturnsAnyOfThem()
    {
        var requests = new[]
        {
            Create(1, type: RequestType.Legal),
            Create(2, type: RequestType.Payment),
            Create(3, type: RequestType.General)
        };

        var result = requests.AsQueryable().ApplyFilters(new SearchRequestsQuery
        {
            Type = [RequestType.Legal, RequestType.Payment]
        });

        Assert.Equal([1, 2], Ids(result));
    }

    [Fact]
    public void ApplyFilters_CreatedFrom_ExcludesEarlierDays()
    {
        var requests = new[]
        {
            Create(1, createdAt: Day.AddTicks(-1)),
            Create(2, createdAt: Day)
        };

        var result = requests.AsQueryable().ApplyFilters(new SearchRequestsQuery
        {
            CreatedFrom = DateOnly.FromDateTime(Day)
        });

        Assert.Equal([2], Ids(result));
    }

    [Fact]
    public void ApplyFilters_CreatedTo_IncludesWholeDay()
    {
        var requests = new[]
        {
            Create(1, createdAt: Day.AddDays(1).AddTicks(-1)),
            Create(2, createdAt: Day.AddDays(1))
        };

        var result = requests.AsQueryable().ApplyFilters(new SearchRequestsQuery
        {
            CreatedTo = DateOnly.FromDateTime(Day)
        });

        Assert.Equal([1], Ids(result));
    }

    [Fact]
    public void ApplySort_SeveralFields_OrdersTiesByTheNextField()
    {
        var requests = new[]
        {
            Create(1, status: RequestStatus.New, createdAt: Day),
            Create(2, status: RequestStatus.Completed, createdAt: Day),
            Create(3, status: RequestStatus.New, createdAt: Day.AddDays(1))
        };

        var result = requests.AsQueryable().ApplySort(
        [
            new RequestSort(RequestSortField.Status, SortDirection.Asc),
            new RequestSort(RequestSortField.CreatedAt, SortDirection.Desc)
        ]);

        Assert.Equal([3, 1, 2], Ids(result));
    }

    // Without a fixed order for equal values, the same request could show up on two pages.
    [Fact]
    public void ApplySort_EqualValues_OrdersByIdAscending()
    {
        var requests = new[] { Create(2), Create(3), Create(1) };

        var result = requests.AsQueryable().ApplySort([new RequestSort(RequestSortField.CreatedAt, SortDirection.Desc)]);

        Assert.Equal([1, 2, 3], Ids(result));
    }

    private static int[] Ids(IQueryable<Request> requests)
        => requests.Select(x => x.Id).ToArray();

    private static Request Create(
        int id,
        int ownerId = 1,
        int? assignedTo = null,
        RequestStatus status = RequestStatus.New,
        RequestType type = RequestType.General,
        DateTime? createdAt = null)
        => new()
        {
            Id = id,
            RequestNumber = $"REQ-{id:000000}",
            OwnerId = ownerId,
            AssignedToUserId = assignedTo,
            Status = status,
            RequestType = type,
            CreatedAt = createdAt ?? Day
        };
}
