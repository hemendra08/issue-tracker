using System.Net;
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
    public async Task RequestIdHeader_IsGeneratedAndReturnedInResponse()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.True(response.Headers.Contains("X-Request-ID"));
        var requestId = response.Headers.GetValues("X-Request-ID").First();
        Assert.False(string.IsNullOrWhiteSpace(requestId));
    }

    [Fact]
    public async Task CustomRequestIdHeader_IsPreservedInResponse()
    {
        var customId = "custom-trace-" + Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Add("X-Request-ID", customId);

        var response = await _client.SendAsync(request);

        Assert.True(response.Headers.Contains("X-Request-ID"));
        Assert.Equal(customId, response.Headers.GetValues("X-Request-ID").First());
    }
}
