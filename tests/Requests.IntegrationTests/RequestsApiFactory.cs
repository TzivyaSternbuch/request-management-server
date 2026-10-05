using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Requests.Api.Authentication;
using Requests.Domain.Entities;
using Requests.Infrastructure.Persistence;

namespace Requests.IntegrationTests;

// Starts the real API in memory on an empty SQLite database, so requests go through
// routing, authentication, validation, the service, the repository and real SQL.
public class RequestsApiFactory : WebApplicationFactory<Program>
{
    private const string TestingEnvironment = "Testing";

    // An in-memory SQLite database lives only while its connection is open,
    // so one connection stays open for the whole life of the factory.
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public async Task SeedAsync(params Request[] requests)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RequestsDbContext>();
        db.Requests.AddRange(requests);
        await db.SaveChangesAsync();
    }

    // The API only accepts users that exist in the database, so the user is added first.
    public async Task<HttpClient> CreateClientForNewUserAsync(int userId, bool isAdministrator = false)
    {
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RequestsDbContext>();
            db.Users.Add(new User { Id = userId, IsAdministrator = isAdministrator });
            await db.SaveChangesAsync();
        }

        var client = CreateClient();
        client.DefaultRequestHeaders.Add(
            HeaderUserAuthenticationHandler.UserIdHeader,
            userId.ToString(CultureInfo.InvariantCulture));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment(TestingEnvironment);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<RequestsDbContext>>();
            services.AddDbContext<RequestsDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _connection.Dispose();
    }
}
