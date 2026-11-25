using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace SmartFlow.Api.Tests.Endpoints;

/// <summary>
/// Starts the real API in memory for integration tests, backed by a real SQLite database.
/// By default every factory gets its own private in-memory database, so tests are isolated.
/// Pass a file path to test behaviour across application restarts.
/// </summary>
public sealed class SmartFlowApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "test-api-key";

    // An in-memory SQLite database lives only while at least one connection is open.
    // This connection keeps it alive for the lifetime of the factory.
    private readonly SqliteConnection? _keepAliveConnection;

    public SmartFlowApiFactory(string? databaseFile = null)
    {
        if (databaseFile is null)
        {
            ConnectionString = $"Data Source=smartflow-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            _keepAliveConnection = new SqliteConnection(ConnectionString);
            _keepAliveConnection.Open();
        }
        else
        {
            ConnectionString = $"Data Source={databaseFile}";
        }
    }

    /// <summary>The SQLite connection string the API under test uses.</summary>
    public string ConnectionString { get; }

    /// <summary>The API key the server is configured with. Empty string = not configured.</summary>
    public string ConfiguredApiKey { get; init; } = ApiKey;

    /// <summary>A controllable clock, so timestamps and ordering are deterministic.</summary>
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("SmartFlow:ApiKey", ConfiguredApiKey);
        builder.UseSetting("ConnectionStrings:SmartFlow", ConnectionString);
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Time));
    }

    public HttpClient CreateAuthorizedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAliveConnection?.Dispose();
        }
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
