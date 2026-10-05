namespace Requests.Application.Users;

public interface IUserService
{
    Task<CurrentUser?> FindUserAsync(int userId, CancellationToken cancellationToken = default);
}
