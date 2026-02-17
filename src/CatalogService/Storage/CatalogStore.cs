using System.Collections.Concurrent;
using Shared.Contracts;

namespace CatalogService.Storage;

public sealed class CatalogStore
{
    private readonly ConcurrentDictionary<Guid, CatalogItem> _items = new();

    public CatalogStore()
    {
        // Fixed IDs make it easier to test across services.
        var seed = new[]
        {
            new CatalogItem(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Coffee", 3.50m),
            new CatalogItem(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Tea", 2.75m),
            new CatalogItem(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Sandwich", 6.25m),
        };

        foreach (var item in seed)
        {
            _items[item.Id] = item;
        }
    }

    public IReadOnlyCollection<CatalogItem> GetAll() => _items.Values.OrderBy(i => i.Name).ToArray();

    public bool TryGet(Guid id, out CatalogItem? item) => _items.TryGetValue(id, out item);
}

