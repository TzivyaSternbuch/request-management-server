using Microsoft.EntityFrameworkCore;
using Requests.Application.Common;
using Requests.Application.Requests;
using Requests.Application.Users;
using Requests.Domain.Entities;
using Requests.Infrastructure.Persistence;

namespace Requests.Infrastructure.Repositories;

public class RequestRepository : IRequestRepository
{
    private readonly RequestsDbContext _db;

    public RequestRepository(RequestsDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Request>> SearchAsync(
        SearchRequestsQuery query,
        CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        var filtered = _db.Requests
            .AsNoTracking()
            .VisibleTo(currentUser)
            .ApplyFilters(query);

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await filtered
            .ApplySort(query.SortBy, query.SortDir)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Request>(items, totalCount, query.Page, query.PageSize);
    }
}
