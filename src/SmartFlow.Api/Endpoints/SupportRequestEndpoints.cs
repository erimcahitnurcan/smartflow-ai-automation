using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using SmartFlow.Api.Contracts;
using SmartFlow.Api.Data;
using SmartFlow.Api.Models;

namespace SmartFlow.Api.Endpoints;

public static class SupportRequestEndpoints
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static IEndpointRouteBuilder MapSupportRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/requests");

        // Only writing requires the API key (called by n8n).
        group.MapPost("/", Create).AddEndpointFilter<ApiKeyEndpointFilter>();
        group.MapGet("/", List);
        group.MapGet("/{id:guid}", GetById);

        return app;
    }

    private static IResult Create(CreateSupportRequestDto dto, InMemorySupportRequestStore store, TimeProvider time)
    {
        var errors = Validate(dto);
        if (errors is not null)
        {
            return TypedResults.ValidationProblem(errors);
        }

        // After successful validation the required values are guaranteed to be present.
        var request = new SupportRequest
        {
            Id = Guid.NewGuid(),
            OriginalText = dto.OriginalText!,
            SenderEmail = dto.SenderEmail,
            Category = dto.Category!.Value,
            Priority = dto.Priority!.Value,
            Summary = dto.Summary!,
            SuggestedAction = dto.SuggestedAction!,
            ModelName = dto.ModelName!,
            CreatedAtUtc = time.GetUtcNow().UtcDateTime
        };

        store.Add(request);

        return TypedResults.Created($"/api/requests/{request.Id}", ToResponse(request));
    }

    private static IResult GetById(Guid id, InMemorySupportRequestStore store)
    {
        var request = store.Find(id);
        return request is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(request));
    }

    private static IResult List(
        InMemorySupportRequestStore store,
        RequestCategory? category,
        RequestPriority? priority,
        int page = 1,
        int pageSize = DefaultPageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["paging"] = [$"page must be >= 1 and pageSize must be between 1 and {MaxPageSize}."]
            });
        }

        var query = store.All();
        if (category is not null)
        {
            query = query.Where(r => r.Category == category);
        }
        if (priority is not null)
        {
            query = query.Where(r => r.Priority == priority);
        }

        var filtered = query.OrderByDescending(r => r.CreatedAtUtc).ToList();
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponse)
            .ToList();

        return TypedResults.Ok(new PagedResponse<SupportRequestResponse>(items, page, pageSize, filtered.Count));
    }

    private static SupportRequestResponse ToResponse(SupportRequest r) => new(
        r.Id, r.OriginalText, r.SenderEmail, r.Category, r.Priority,
        r.Summary, r.SuggestedAction, r.ModelName, r.CreatedAtUtc);

    // Runs the DataAnnotations rules of the DTO and returns errors keyed by camelCase JSON field name,
    // or null when the object is valid.
    private static Dictionary<string, string[]>? Validate(object model)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true))
        {
            return null;
        }

        return results
            .SelectMany(r => r.MemberNames.Select(member => (Field: JsonNamingPolicy.CamelCase.ConvertName(member), Message: r.ErrorMessage ?? "Invalid value.")))
            .GroupBy(e => e.Field)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());
    }
}
