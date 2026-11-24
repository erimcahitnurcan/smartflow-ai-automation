namespace SmartFlow.Api.Models;

/// <summary>
/// A classified support request as it is stored.
/// In Phase 3 this class becomes the EF Core entity for the SQLite table.
/// </summary>
public sealed class SupportRequest
{
    public Guid Id { get; set; }
    public required string OriginalText { get; set; }
    public string? SenderEmail { get; set; }
    public RequestCategory Category { get; set; }
    public RequestPriority Priority { get; set; }
    public required string Summary { get; set; }
    public required string SuggestedAction { get; set; }
    public required string ModelName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
