using System.Text.Json.Serialization;
using SmartFlow.Api.Data;
using SmartFlow.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Read and write enums as strings ("Billing") instead of numbers (0).
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Injected clock: production uses the system time, tests use a fake time.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<InMemorySupportRequestStore>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapSupportRequestEndpoints();

app.Run();
