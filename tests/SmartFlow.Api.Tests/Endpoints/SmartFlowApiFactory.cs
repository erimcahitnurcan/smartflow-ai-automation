using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace SmartFlow.Api.Tests.Endpoints;

/// <summary>
/// Starts the real API in memory for integration tests.
/// Each test creates its own factory, so every test gets an empty store.
/// </summary>
public sealed class SmartFlowApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "test-api-key";

    /// <summary>The API key the server is configured with. Empty string = not configured.</summary>
    public string ConfiguredApiKey { get; init; } = ApiKey;

    /// <summary>A controllable clock, so timestamps and ordering are deterministic.</summary>
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("SmartFlow:ApiKey", ConfiguredApiKey);
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Time));
    }

    public HttpClient CreateAuthorizedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        return client;
    }
}

internal static class TestJson
{
    // Same conventions as the API: camelCase names, enums as strings.
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
