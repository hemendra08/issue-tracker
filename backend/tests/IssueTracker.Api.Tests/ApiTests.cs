using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IssueTracker.Api.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealthLive_ReturnsOkWithStatusHealthy()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("Healthy", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("IssueTracker.Api", doc.RootElement.GetProperty("service").GetString());
    }

    [Fact]
    public async Task GetIdentityPing_ReturnsPong()
    {
        var response = await _client.GetAsync("/api/v1/identity/health/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("pong", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("Identity", doc.RootElement.GetProperty("feature").GetString());
    }

    [Fact]
    public async Task GetProjectsPing_ReturnsPong()
    {
        var response = await _client.GetAsync("/api/v1/projects/health/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("pong", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("Projects", doc.RootElement.GetProperty("feature").GetString());
    }

    [Fact]
    public async Task GetIssuesPing_ReturnsPong()
    {
        var response = await _client.GetAsync("/api/v1/issues/health/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("pong", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("Issues", doc.RootElement.GetProperty("feature").GetString());
    }

    [Fact]
    public async Task RequestIdHeader_IsGeneratedAndReturnedInResponse()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.True(response.Headers.Contains("X-Request-ID"));
        var requestId = response.Headers.GetValues("X-Request-ID").First();
        Assert.False(string.IsNullOrWhiteSpace(requestId));
    }

    [Fact]
    public async Task CustomRequestIdHeader_IsPreservedInResponse()
    {
        var customId = "custom-trace-" + Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Request-ID", customId);

        var response = await _client.SendAsync(request);

        Assert.True(response.Headers.Contains("X-Request-ID"));
        Assert.Equal(customId, response.Headers.GetValues("X-Request-ID").First());
    }
}
