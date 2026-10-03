using Requests.Infrastructure;
using Requests.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure();

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
    DbSeeder.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(FrontendDevCors);
}

app.MapControllers();

app.Run();

public partial class Program { }
