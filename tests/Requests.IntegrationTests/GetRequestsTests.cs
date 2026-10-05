using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Requests.Api.Authentication;
using Requests.Application.Common;
using Requests.Application.Requests;
using Requests.Domain.Entities;
using Xunit;

namespace Requests.IntegrationTests;

// A new factory per test (xUnit creates the class for every test), so each test has its own empty database.
public class GetRequestsTests : IDisposable
{
    private const string RequestsUrl = "/api/requests";
    private const string ProblemJsonContentType = "application/problem+json";
    private const int UserId = 1;
    private const int OtherUserId = 2;

    private static readonly DateTime Day = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly RequestsApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    // "99" is a well-formed id of a user that does not exist (the database has no users here).
    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("abc")]
    [InlineData("99")]
    public async Task GetRequests_MissingInvalidOrUnknownUserId_Returns401ProblemDetails(string? userId)
    {
        var client = _factory.CreateClient();
        if (userId is not null)
            client.DefaultRequestHeaders.Add(HeaderUserAuthenticationHandler.UserIdHeader, userId);

        var response = await client.GetAsync(RequestsUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemJsonContentType, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetRequests_InvalidQuery_Returns400WithEveryError()
    {
        var client = await _factory.CreateClientForNewUserAsync(UserId);

        var response = await client.GetAsync($"{RequestsUrl}?pageSize=0&createdFrom=2026-03-02&createdTo=2026-03-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemJsonContentType, response.Content.Headers.ContentType?.MediaType);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("pageSize", out _));
        Assert.True(errors.TryGetProperty("createdFrom", out _));
    }

    // Pins the JSON shape that the front end's requestModels.ts relies on.
    [Fact]
    public async Task GetRequests_ValidRequest_ReturnsCamelCaseJsonWithEnumNames()
    {
        await _factory.SeedAsync(Create(1, status: RequestStatus.InProgress, type: RequestType.Legal));
        var client = await _factory.CreateClientForNewUserAsync(UserId);

        var response = await client.GetAsync(RequestsUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal(1, json.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, json.GetProperty("page").GetInt32());
        Assert.Equal(SearchRequestsQuery.DefaultPageSize, json.GetProperty("pageSize").GetInt32());
        var item = json.GetProperty("items")[0];
        Assert.Equal("REQ-000001", item.GetProperty("requestNumber").GetString());
        Assert.Equal("InProgress", item.GetProperty("status").GetString());
        Assert.Equal("Legal", item.GetProperty("requestType").GetString());
    }

    // The visibility rule itself is unit tested; this checks that the administrator flag
    // stored on the user reaches it.
    [Fact]
    public async Task GetRequests_Administrator_ReturnsAllRequests()
    {
        await _factory.SeedAsync(
            Create(1, ownerId: OtherUserId),
            Create(2, ownerId: OtherUserId, assignedTo: OtherUserId));
        var client = await _factory.CreateClientForNewUserAsync(UserId, isAdministrator: true);

        var result = await SearchAsync(client, "?sortBy=RequestNumber&sortDir=Asc");

        Assert.Equal(["REQ-000001", "REQ-000002"], RequestNumbers(result));
    }

    [Fact]
    public async Task GetRequests_SeveralFilters_ReturnsOnlyMatchingRequests()
    {
        await _factory.SeedAsync(
            Create(11, status: RequestStatus.New, type: RequestType.Legal, createdAt: Day),
            Create(12, status: RequestStatus.Completed, type: RequestType.Legal, createdAt: Day),
            Create(13, status: RequestStatus.New, type: RequestType.Payment, createdAt: Day),
            Create(14, status: RequestStatus.New, type: RequestType.Legal, createdAt: Day.AddDays(2)),
            Create(21, status: RequestStatus.New, type: RequestType.Legal, createdAt: Day));
        var client = await _factory.CreateClientForNewUserAsync(UserId);

        var result = await SearchAsync(
            client,
            "?requestNumber=req-00001&status=New&status=InProgress&type=Legal&createdFrom=2026-03-01&createdTo=2026-03-02");

        Assert.Equal(["REQ-000011"], RequestNumbers(result));
    }

    [Fact]
    public async Task GetRequests_SecondPageSortedByRequestNumber_ReturnsThatPageAndTotalCount()
    {
        await _factory.SeedAsync(Create(5), Create(3), Create(1), Create(4), Create(2));
        var client = await _factory.CreateClientForNewUserAsync(UserId);

        var result = await SearchAsync(client, "?sortBy=RequestNumber&sortDir=Asc&page=2&pageSize=2");

        Assert.Equal(["REQ-000003", "REQ-000004"], RequestNumbers(result));
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    private static async Task<PagedResult<RequestDto>> SearchAsync(HttpClient client, string queryString)
    {
        var response = await client.GetAsync(RequestsUrl + queryString);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<RequestDto>>(JsonOptions);
        Assert.NotNull(result);
        return result;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    private static string[] RequestNumbers(PagedResult<RequestDto> result)
        => result.Items.Select(x => x.RequestNumber).ToArray();

    private static Request Create(
        int number,
        int ownerId = UserId,
        int? assignedTo = null,
        RequestStatus status = RequestStatus.New,
        RequestType type = RequestType.General,
        DateTime? createdAt = null)
        => new()
        {
            RequestNumber = $"REQ-{number:000000}",
            OwnerId = ownerId,
            AssignedToUserId = assignedTo,
            Status = status,
            RequestType = type,
            CreatedAt = createdAt ?? Day,
            UpdatedAt = createdAt ?? Day
        };
}
