using System.ComponentModel.DataAnnotations;
using SmartFlow.Api.Models;

namespace SmartFlow.Api.Contracts;

/// <summary>
/// Payload sent by n8n to POST /api/requests after the LLM has classified a request.
/// Every property is nullable on purpose: a missing JSON field must fail validation
/// instead of silently becoming a default value (for example Category = Billing).
/// </summary>
public sealed record CreateSupportRequestDto
{
    [Required]
    [StringLength(5000)]
    public string? OriginalText { get; init; }

    [EmailAddress]
    [StringLength(254)]
    public string? SenderEmail { get; init; }

    [Required]
    [EnumDataType(typeof(RequestCategory))]
    public RequestCategory? Category { get; init; }

    [Required]
    [EnumDataType(typeof(RequestPriority))]
    public RequestPriority? Priority { get; init; }

    [Required]
    [StringLength(300)]
    public string? Summary { get; init; }

    [Required]
    [StringLength(500)]
    public string? SuggestedAction { get; init; }

    [Required]
    [StringLength(100)]
    public string? ModelName { get; init; }
}
