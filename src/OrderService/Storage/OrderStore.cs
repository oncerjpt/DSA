using System.Collections.Concurrent;
using Shared.Contracts;

namespace OrderService.Storage;

public sealed class OrderStore
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public void Upsert(Order order) => _orders[order.Id] = order;

    public bool TryGet(Guid id, out Order? order) => _orders.TryGetValue(id, out order);
}

