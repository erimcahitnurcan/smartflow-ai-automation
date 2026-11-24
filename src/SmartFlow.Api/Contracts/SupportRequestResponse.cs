using SmartFlow.Api.Models;

namespace SmartFlow.Api.Contracts;

/// <summary>
/// A stored support request as returned by the API.
/// </summary>
public sealed record SupportRequestResponse(
    Guid Id,
    string OriginalText,
    string? SenderEmail,
    RequestCategory Category,
    RequestPriority Priority,
    string Summary,
    string SuggestedAction,
    string ModelName,
    DateTime CreatedAtUtc);
