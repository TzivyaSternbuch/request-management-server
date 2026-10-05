using System.Linq.Expressions;
using Requests.Application.Common;
using Requests.Application.Users;
using Requests.Domain.Entities;

namespace Requests.Application.Requests;

public static class RequestQueryableExtensions
{
    public static IQueryable<Request> VisibleTo(
        this IQueryable<Request> requests,
        CurrentUser currentUser)
    {
        if (currentUser.IsAdministrator)
            return requests;

        var userId = currentUser.UserId;
        return requests.Where(x => x.OwnerId == userId || x.AssignedToUserId == userId);
    }

    public static IQueryable<Request> ApplyFilters(
        this IQueryable<Request> requests,
        SearchRequestsQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.RequestNumber))
        {
            var term = query.RequestNumber.Trim().ToUpperInvariant();
            requests = requests.Where(x => x.RequestNumber.Contains(term));
        }

        if (query.Status.Length > 0)
        {
            var statuses = query.Status;
            requests = requests.Where(x => statuses.Contains(x.Status));
        }

        if (query.Type.Length > 0)
        {
            var types = query.Type;
            requests = requests.Where(x => types.Contains(x.RequestType));
        }

        if (query.CreatedFrom is DateOnly createdFrom)
        {
            var startOfDay = createdFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            requests = requests.Where(x => x.CreatedAt >= startOfDay);
        }

        if (query.CreatedTo is DateOnly createdTo)
        {
            // The last moment of the day instead of "next day" so DateOnly.MaxValue cannot overflow.
            var endOfDay = createdTo.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            requests = requests.Where(x => x.CreatedAt <= endOfDay);
        }

        return requests;
    }

    public static IQueryable<Request> ApplySort(
        this IQueryable<Request> requests,
        RequestSortField sortBy,
        SortDirection sortDirection)
    {
        return sortBy switch
        {
            RequestSortField.CreatedAt => OrderByThenById(requests, x => x.CreatedAt, sortDirection),
            RequestSortField.RequestNumber => OrderByThenById(requests, x => x.RequestNumber, sortDirection),
            RequestSortField.Status => OrderByThenById(requests, x => x.Status, sortDirection),
            RequestSortField.Type => OrderByThenById(requests, x => x.RequestType, sortDirection),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        };
    }

    public static IQueryable<RequestDto> ToDto(this IQueryable<Request> requests)
        => requests.Select(x => new RequestDto(
            x.Id,
            x.RequestNumber,
            x.CustomerId,
            x.OwnerId,
            x.AssignedToUserId,
            x.Status,
            x.RequestType,
            x.CreatedAt));

    private static IQueryable<Request> OrderByThenById<TKey>(
        IQueryable<Request> requests,
        Expression<Func<Request, TKey>> key,
        SortDirection sortDirection)
    {
        return sortDirection == SortDirection.Asc
            ? requests.OrderBy(key).ThenBy(x => x.Id)
            : requests.OrderByDescending(key).ThenByDescending(x => x.Id);
    }
}
