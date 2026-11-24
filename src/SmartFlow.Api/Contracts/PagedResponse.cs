namespace SmartFlow.Api.Contracts;

/// <summary>
/// One page of a list result plus the information a client needs to request the next page.
/// </summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
