using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SmartFlow.Api.Data;
using SmartFlow.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Read and write enums as strings ("Billing") instead of numbers (0).
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Injected clock: production uses the system time, tests use a fake time.
builder.Services.AddSingleton(TimeProvider.System);

// SQLite database. The connection string comes from configuration
// (appsettings.json locally, the ConnectionStrings__SmartFlow environment variable in Docker).
builder.Services.AddDbContext<SmartFlowDbContext>((services, options) =>
    options.UseSqlite(services.GetRequiredService<IConfiguration>().GetConnectionString("SmartFlow")));

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Create or upgrade the database schema at startup.
// Fine for a single-instance MVP; larger systems run migrations as a separate deployment step.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<SmartFlowDbContext>().Database.Migrate();
}

app.MapHealthChecks("/health");
app.MapSupportRequestEndpoints();

app.Run();
