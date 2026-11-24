using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmartFlow.Api.Contracts;
using SmartFlow.Api.Models;

namespace SmartFlow.Api.Tests.Endpoints;

public sealed class SupportRequestEndpointsTests : IDisposable
{
    private static readonly DateTime StartTimeUtc = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

    private readonly SmartFlowApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    // The JSON shape n8n will send. Enums are strings, exactly like the LLM output.
    private static object Payload(
        string category = "Billing",
        string priority = "High",
        string? summary = "Customer was charged twice for the November invoice.") => new
    {
        originalText = "I was charged twice for my November invoice.",
        senderEmail = "customer@example.com",
        category,
        priority,
        summary,
        suggestedAction = "Check the payment provider and refund the duplicate charge.",
        modelName = "llama3.1:8b"
    };

    private async Task<SupportRequestResponse> CreateAsync(object payload)
    {
        var response = await _factory.CreateAuthorizedClient().PostAsJsonAsync("/api/requests", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SupportRequestResponse>(TestJson.Options))!;
    }

    private async Task<PagedResponse<SupportRequestResponse>> ListAsync(string query = "")
    {
        var response = await _factory.CreateClient().GetAsync($"/api/requests{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<SupportRequestResponse>>(TestJson.Options))!;
    }

    // ---------- POST /api/requests ----------

    [Fact]
    public async Task Post_with_valid_payload_returns_201_and_the_stored_request()
    {
        var response = await _factory.CreateAuthorizedClient().PostAsJsonAsync("/api/requests", Payload());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SupportRequestResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal($"/api/requests/{body.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("I was charged twice for my November invoice.", body.OriginalText);
        Assert.Equal("customer@example.com", body.SenderEmail);
        Assert.Equal(RequestCategory.Billing, body.Category);
        Assert.Equal(RequestPriority.High, body.Priority);
        Assert.Equal("Customer was charged twice for the November invoice.", body.Summary);
        Assert.Equal("Check the payment provider and refund the duplicate charge.", body.SuggestedAction);
        Assert.Equal("llama3.1:8b", body.ModelName);
        Assert.Equal(StartTimeUtc, body.CreatedAtUtc);
    }

    [Fact]
    public async Task Post_returns_enums_as_strings_in_json()
    {
        var response = await _factory.CreateAuthorizedClient().PostAsJsonAsync("/api/requests", Payload());

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Billing", json.RootElement.GetProperty("category").GetString());
        Assert.Equal("High", json.RootElement.GetProperty("priority").GetString());
    }

    [Fact]
    public async Task Post_without_api_key_returns_401_and_stores_nothing()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/requests", Payload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, (await ListAsync()).TotalCount);
    }

    [Fact]
    public async Task Post_with_wrong_api_key_returns_401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        var response = await client.PostAsJsonAsync("/api/requests", Payload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_is_rejected_when_server_has_no_api_key_configured()
    {
        // Fail closed: a missing configuration must never mean "no protection".
        using var factory = new SmartFlowApiFactory { ConfiguredApiKey = "" };
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "");

        var response = await client.PostAsJsonAsync("/api/requests", Payload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_with_missing_summary_returns_400_with_field_error()
    {
        var response = await _factory.CreateAuthorizedClient().PostAsJsonAsync("/api/requests", Payload(summary: null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("summary", out _));
        Assert.Equal(0, (await ListAsync()).TotalCount);
    }

    [Fact]
    public async Task Post_with_unknown_category_returns_400()
    {
        var response = await _factory.CreateAuthorizedClient().PostAsJsonAsync("/api/requests", Payload(category: "Sales"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, (await ListAsync()).TotalCount);
    }

    // ---------- GET /api/requests/{id} ----------

    [Fact]
    public async Task Get_by_id_returns_the_created_request()
    {
        var created = await CreateAsync(Payload(category: "Technical"));

        var response = await _factory.CreateClient().GetAsync($"/api/requests/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SupportRequestResponse>(TestJson.Options);
        Assert.Equal(created, body);
    }

    [Fact]
    public async Task Get_by_id_returns_404_for_unknown_id()
    {
        var response = await _factory.CreateClient().GetAsync($"/api/requests/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- GET /api/requests ----------

    [Fact]
    public async Task List_returns_newest_request_first()
    {
        await CreateAsync(Payload(summary: "older"));
        _factory.Time.Advance(TimeSpan.FromMinutes(1));
        await CreateAsync(Payload(summary: "newer"));

        var page = await ListAsync();

        Assert.Equal(["newer", "older"], page.Items.Select(i => i.Summary));
    }

    [Fact]
    public async Task List_filters_by_category()
    {
        await CreateAsync(Payload(category: "Billing"));
        await CreateAsync(Payload(category: "Technical"));

        var page = await ListAsync("?category=Technical");

        Assert.Equal(1, page.TotalCount);
        Assert.Equal(RequestCategory.Technical, Assert.Single(page.Items).Category);
    }

    [Fact]
    public async Task List_filters_by_priority()
    {
        await CreateAsync(Payload(priority: "Low"));
        await CreateAsync(Payload(priority: "High"));

        var page = await ListAsync("?priority=Low");

        Assert.Equal(1, page.TotalCount);
        Assert.Equal(RequestPriority.Low, Assert.Single(page.Items).Priority);
    }

    [Fact]
    public async Task List_returns_requested_page_and_total_count()
    {
        await CreateAsync(Payload(summary: "first"));
        _factory.Time.Advance(TimeSpan.FromMinutes(1));
        await CreateAsync(Payload(summary: "second"));
        _factory.Time.Advance(TimeSpan.FromMinutes(1));
        await CreateAsync(Payload(summary: "third"));

        var page = await ListAsync("?page=2&pageSize=2");

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal("first", Assert.Single(page.Items).Summary);
    }

    [Fact]
    public async Task List_uses_page_size_20_by_default()
    {
        var page = await ListAsync();

        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
    }

    [Theory]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?page=0")]
    public async Task List_rejects_invalid_paging_with_400(string query)
    {
        var response = await _factory.CreateClient().GetAsync($"/api/requests{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
