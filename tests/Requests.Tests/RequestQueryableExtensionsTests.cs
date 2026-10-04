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
            Create(3, ownerId: 3, assignedTo: 5)
        };

        var result = requests.AsQueryable().VisibleTo(new CurrentUser(1, IsAdministrator: false));

        Assert.Equal([1, 2], Ids(result));
    }

    [Fact]
    public void VisibleTo_RegularUser_HidesUnassignedRequestsOfOthers()
    {
        var requests = new[]
        {
            Create(1, ownerId: 3, assignedTo: null)
        };

        var result = requests.AsQueryable().VisibleTo(new CurrentUser(1, IsAdministrator: false));

        Assert.Empty(result);
    }

    [Fact]
    public void ApplyFilters_NoFilters_ReturnsAllRequests()
    {
        var requests = new[] { Create(1), Create(2) };

        var result = requests.AsQueryable().ApplyFilters(new SearchRequestsQuery());

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
    public void ApplyFilters_RequestNumberLowerCase_ReturnsMatchingRequests()
    {
        var requests = new[] { Create(1), Create(2) };

        var result = requests.AsQueryable().ApplyFilters(new SearchRequestsQuery { RequestNumber = "req-000001" });

        Assert.Equal([1], Ids(result));
    }

    [Fact]
    public void ApplyFilters_RequestNumberBlank_IsIgnored()
    {
        var requests = new[] { Create(1), Create(2) };

        var result = requests.AsQueryable().ApplyFilters(new SearchRequestsQuery { RequestNumber = "   " });

        Assert.Equal([1, 2], Ids(result));
    }

    [Fact]
    public void ApplyFilters_OneStatus_ReturnsOnlyThatStatus()
    {
        var requests = new[]
        {
            Create(1, status: RequestStatus.New),
            Create(2, status: RequestStatus.Completed)
        };

        var result = requests.AsQueryable().ApplyFilters(new SearchRequestsQuery { Status = [RequestStatus.New] });

        Assert.Equal([1], Ids(result));
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
    public void ApplyFilters_CustomerId_ReturnsOnlyThatCustomer()
    {
        var requests = new[] { Create(1, customerId: 8), Create(2, customerId: 9) };

        var result = requests.AsQueryable().ApplyFilters(new SearchRequestsQuery { CustomerId = 8 });

        Assert.Equal([1], Ids(result));
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
    public void ApplyFilters_CreatedToMaxDate_DoesNotThrow()
    {
        var requests = new[] { Create(1) };

        var result = requests.AsQueryable().ApplyFilters(new SearchRequestsQuery { CreatedTo = DateOnly.MaxValue });

        Assert.Equal([1], Ids(result));
    }

    [Fact]
    public void ApplyFilters_SeveralFilters_CombinesWithAnd()
    {
        var requests = new[]
        {
            Create(1, status: RequestStatus.New, customerId: 5),
            Create(2, status: RequestStatus.New, customerId: 6),
            Create(3, status: RequestStatus.Completed, customerId: 5)
        };

        var result = requests.AsQueryable().ApplyFilters(new SearchRequestsQuery
        {
            Status = [RequestStatus.New],
            CustomerId = 5
        });

        Assert.Equal([1], Ids(result));
    }

    [Theory]
    [InlineData(RequestSortField.CreatedAt, SortDirection.Asc, new[] { 2, 1, 3 })]
    [InlineData(RequestSortField.CreatedAt, SortDirection.Desc, new[] { 3, 1, 2 })]
    [InlineData(RequestSortField.RequestNumber, SortDirection.Asc, new[] { 1, 2, 3 })]
    [InlineData(RequestSortField.RequestNumber, SortDirection.Desc, new[] { 3, 2, 1 })]
    [InlineData(RequestSortField.Status, SortDirection.Asc, new[] { 2, 3, 1 })]
    [InlineData(RequestSortField.Status, SortDirection.Desc, new[] { 1, 3, 2 })]
    [InlineData(RequestSortField.Type, SortDirection.Asc, new[] { 3, 1, 2 })]
    [InlineData(RequestSortField.Type, SortDirection.Desc, new[] { 2, 1, 3 })]
    [InlineData(RequestSortField.CustomerId, SortDirection.Asc, new[] { 1, 3, 2 })]
    [InlineData(RequestSortField.CustomerId, SortDirection.Desc, new[] { 2, 3, 1 })]
    public void ApplySort_EachFieldAndDirection_OrdersAccordingly(
        RequestSortField sortBy,
        SortDirection sortDirection,
        int[] expectedIds)
    {
        var requests = new[]
        {
            Create(1, status: RequestStatus.Completed, type: RequestType.Payment, customerId: 1, createdAt: Day.AddDays(1)),
            Create(2, status: RequestStatus.New, type: RequestType.Appeal, customerId: 3, createdAt: Day),
            Create(3, status: RequestStatus.InProgress, type: RequestType.General, customerId: 2, createdAt: Day.AddDays(2))
        };

        var result = requests.AsQueryable().ApplySort(sortBy, sortDirection);

        Assert.Equal(expectedIds, Ids(result));
    }

    [Theory]
    [InlineData(SortDirection.Asc, new[] { 1, 2, 3 })]
    [InlineData(SortDirection.Desc, new[] { 3, 2, 1 })]
    public void ApplySort_EqualValues_OrdersById(SortDirection sortDirection, int[] expectedIds)
    {
        var requests = new[] { Create(2), Create(3), Create(1) };

        var result = requests.AsQueryable().ApplySort(RequestSortField.CreatedAt, sortDirection);

        Assert.Equal(expectedIds, Ids(result));
    }

    private static int[] Ids(IQueryable<Request> requests)
        => requests.Select(x => x.Id).ToArray();

    private static Request Create(
        int id,
        int ownerId = 1,
        int? assignedTo = null,
        RequestStatus status = RequestStatus.New,
        RequestType type = RequestType.General,
        int customerId = 1,
        DateTime? createdAt = null)
        => new()
        {
            Id = id,
            RequestNumber = $"REQ-{id:000000}",
            CustomerId = customerId,
            OwnerId = ownerId,
            AssignedToUserId = assignedTo,
            Status = status,
            RequestType = type,
            CreatedAt = createdAt ?? Day
        };
}
