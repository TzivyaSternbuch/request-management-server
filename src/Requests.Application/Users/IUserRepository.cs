namespace Requests.Application.Users;

public interface IUserRepository
{
    Task<CurrentUser?> FindAsync(int userId, CancellationToken cancellationToken = default);
}
