using Microsoft.EntityFrameworkCore;
using Requests.Application.Users;
using Requests.Infrastructure.Persistence;

namespace Requests.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly RequestsDbContext _db;

    public UserRepository(RequestsDbContext db)
    {
        _db = db;
    }

    public Task<CurrentUser?> FindAsync(int userId, CancellationToken cancellationToken = default)
        => _db.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new CurrentUser(x.Id, x.IsAdministrator))
            .SingleOrDefaultAsync(cancellationToken);
}
