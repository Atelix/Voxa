using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Starter.Abstractions.Models;

namespace Starter.WebApi.UnitTests.Api;

public sealed class ApiTests(TestApi api) : IClassFixture<TestApi>
{
    private readonly HttpClient _client = api.CreateClient();

    [Fact]
    public async Task HealthReportsTheVersion()
    {
        using var response = await _client.GetAsync(new Uri("/api/health", UriKind.Relative));

        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("1.2.3", body.RootElement.GetProperty("version").GetString());
        Assert.Equal("1.2.3", response.Headers.GetValues(AppVersion.HeaderName).Single());
    }

    [Fact]
    public async Task AMissingItemIsAProblemResponse()
    {
        using var response = await _client.GetAsync(new Uri("/api/items/999999", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Item", body.RootElement.GetProperty("entity").GetString());
    }

    [Fact]
    public async Task ADuplicateNameIsAConflict()
    {
        var request = new ItemRequest { Name = $"Duplicate {Guid.NewGuid()}" };
        using var first = await _client.PostAsJsonAsync(new Uri("/api/items", UriKind.Relative), request);
        using var second = await _client.PostAsJsonAsync(new Uri("/api/items", UriKind.Relative), request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task AnInvalidBodyIsABadRequest()
    {
        using var response = await _client.PostAsJsonAsync(new Uri("/api/items", UriKind.Relative), new { name = new string('x', ItemRequest.NameMaxLength + 1) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
