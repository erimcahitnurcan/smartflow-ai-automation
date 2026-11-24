using System.Net;

namespace SmartFlow.Api.Tests.Endpoints;

public sealed class HealthEndpointTests : IDisposable
{
    private readonly SmartFlowApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Health_endpoint_returns_200_without_api_key()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
