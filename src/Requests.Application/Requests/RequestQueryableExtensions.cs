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
        IReadOnlyList<RequestSort> sorts)
    {
        IOrderedQueryable<Request>? ordered = null;

        foreach (var sort in sorts)
        {
            ordered = sort.Field switch
            {
                RequestSortField.CreatedAt => AddSortKey(requests, ordered, x => x.CreatedAt, sort.Direction),
                RequestSortField.RequestNumber => AddSortKey(requests, ordered, x => x.RequestNumber, sort.Direction),
                RequestSortField.Status => AddSortKey(requests, ordered, x => x.Status, sort.Direction),
                RequestSortField.Type => AddSortKey(requests, ordered, x => x.RequestType, sort.Direction),
                _ => throw new ArgumentOutOfRangeException(nameof(sorts), sort.Field, null)
            };
        }

        // Id is unique, so rows with equal values keep a fixed order and pages never overlap.
        return AddSortKey(requests, ordered, x => x.Id, SortDirection.Asc);
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

    // The first key starts the order (OrderBy); each next key only orders the ties left by the ones before (ThenBy).
    private static IOrderedQueryable<Request> AddSortKey<TKey>(
        IQueryable<Request> requests,
        IOrderedQueryable<Request>? ordered,
        Expression<Func<Request, TKey>> key,
        SortDirection sortDirection)
    {
        if (ordered is null)
        {
            return sortDirection == SortDirection.Asc
                ? requests.OrderBy(key)
                : requests.OrderByDescending(key);
        }

        return sortDirection == SortDirection.Asc
            ? ordered.ThenBy(key)
            : ordered.ThenByDescending(key);
    }
}
