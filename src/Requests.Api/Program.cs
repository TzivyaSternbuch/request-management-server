using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Requests.Api.Authentication;
using Requests.Infrastructure;
using Requests.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// By default MVC skips the record-level check (createdFrom > createdTo) when a field
// is already invalid; this reports all validation errors at once.
builder.Services
    .AddControllers(options => options.ValidateComplexTypesIfChildValidationFails = true)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Lets the Swagger "Authorize" button send the simulated user headers.
    string[] userHeaders =
    [
        HeaderUserAuthenticationHandler.UserIdHeader,
        HeaderUserAuthenticationHandler.IsAdminHeader
    ];
    foreach (var header in userHeaders)
    {
        options.AddSecurityDefinition(header, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = header
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = header }
            }] = []
        });
    }
});

builder.Services
    .AddAuthentication(HeaderUserAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, HeaderUserAuthenticationHandler>(
        HeaderUserAuthenticationHandler.SchemeName,
        configureOptions: null);
builder.Services.AddAuthorization();

const string RequestsConnectionStringName = "Requests";
var connectionString = builder.Configuration.GetConnectionString(RequestsConnectionStringName)
    ?? throw new InvalidOperationException(
        $"Connection string '{RequestsConnectionStringName}' is missing.");
builder.Services.AddInfrastructure(connectionString, AppContext.BaseDirectory);

// The React dev server normally proxies /api, so CORS is a fallback for
// calling the API directly from http://localhost:5173 during development.
const string FrontendDevCors = "FrontendDev";
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendDevCors, policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RequestsDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(FrontendDevCors);
}

app.UseAuthentication();
app.UseAuthorization();

// Secure by default: every controller needs a user unless marked [AllowAnonymous].
app.MapControllers().RequireAuthorization();

app.Run();

public partial class Program { }
