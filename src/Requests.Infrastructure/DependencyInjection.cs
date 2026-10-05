using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Requests.Application.Requests;
using Requests.Infrastructure.Persistence;
using Requests.Infrastructure.Repositories;

namespace Requests.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string databaseDirectory)
    {
        services.AddDbContext<RequestsDbContext>(options =>
            options.UseSqlite(ResolveDatabasePath(connectionString, databaseDirectory)));

        services.AddScoped<IRequestRepository, RequestRepository>();
        services.AddScoped<IRequestService, RequestService>();

        return services;
    }

    // SQLite resolves a relative file path against the working directory, so starting the app
    // from another folder would silently create and seed a second database.
    private static string ResolveDatabasePath(string connectionString, string databaseDirectory)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
        connectionStringBuilder.DataSource = Path.Combine(databaseDirectory, connectionStringBuilder.DataSource);
        return connectionStringBuilder.ToString();
    }
}
