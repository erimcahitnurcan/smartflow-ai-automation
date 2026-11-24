using System.Collections.Concurrent;
using SmartFlow.Api.Models;

namespace SmartFlow.Api.Data;

/// <summary>
/// Temporary thread-safe storage for Phase 2.
/// Phase 3 replaces it with EF Core + SQLite; data is lost when the app stops.
/// </summary>
public sealed class InMemorySupportRequestStore
{
    private readonly ConcurrentDictionary<Guid, SupportRequest> _items = new();

    public void Add(SupportRequest request) => _items[request.Id] = request;

    public SupportRequest? Find(Guid id) => _items.TryGetValue(id, out var request) ? request : null;

    public IEnumerable<SupportRequest> All() => _items.Values;
}
