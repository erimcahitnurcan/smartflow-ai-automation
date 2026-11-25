using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using SmartFlow.Api.Contracts;

namespace SmartFlow.Api.Tests.Endpoints;

/// <summary>
/// Verifies what Phase 3 adds: requests are stored in SQLite and survive a restart.
/// </summary>
public sealed class PersistenceTests : IDisposable
{
    private readonly string _databaseFile = Path.Combine(Path.GetTempPath(), $"smartflow-test-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        // Pooled connections keep the file locked on Windows; release them before deleting.
        SqliteConnection.ClearAllPools();
        File.Delete(_databaseFile);
    }

    private static object Payload() => new
    {
        originalText = "I cannot log in after resetting my password.",
        category = "Account",
        priority = "Medium",
        summary = "Login fails after password reset.",
        suggestedAction = "Check the account lock status and send a new reset link.",
        modelName = "llama3.1:8b"
    };

    [Fact]
    public async Task Request_is_still_available_after_application_restart()
    {
        SupportRequestResponse created;
        using (var firstRun = new SmartFlowApiFactory(_databaseFile))
        {
            var response = await firstRun.CreateAuthorizedClient().PostAsJsonAsync("/api/requests", Payload());
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            created = (await response.Content.ReadFromJsonAsync<SupportRequestResponse>(TestJson.Options))!;
        }

        using var secondRun = new SmartFlowApiFactory(_databaseFile);
        var fetched = await secondRun.CreateClient().GetAsync($"/api/requests/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        var body = await fetched.Content.ReadFromJsonAsync<SupportRequestResponse>(TestJson.Options);
        Assert.Equal("Login fails after password reset.", body!.Summary);
    }

    [Fact]
    public async Task Category_and_priority_are_stored_as_text_not_numbers()
    {
        // Stored numbers would silently change meaning if someone reorders the enum.
        using var factory = new SmartFlowApiFactory();
        await factory.CreateAuthorizedClient().PostAsJsonAsync("/api/requests", Payload());

        await using var connection = new SqliteConnection(factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Category, Priority FROM SupportRequests";
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("Account", reader.GetString(0));
        Assert.Equal("Medium", reader.GetString(1));
    }

    [Fact]
    public async Task Created_timestamp_read_from_database_is_returned_as_utc()
    {
        // SQLite has no time zone information. Without care, values come back as
        // "Unspecified" and the JSON loses its trailing "Z", which clients misread as local time.
        using var factory = new SmartFlowApiFactory();
        var post = await factory.CreateAuthorizedClient().PostAsJsonAsync("/api/requests", Payload());
        var created = (await post.Content.ReadFromJsonAsync<SupportRequestResponse>(TestJson.Options))!;

        var response = await factory.CreateClient().GetAsync($"/api/requests/{created.Id}");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("2026-11-24T12:00:00Z", json.RootElement.GetProperty("createdAtUtc").GetString());
    }
}
