namespace Requests.Application.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public Task<CurrentUser?> FindUserAsync(int userId, CancellationToken cancellationToken = default)
        => _repository.FindAsync(userId, cancellationToken);
}
