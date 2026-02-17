using System.Collections.Concurrent;
using Shared.Contracts;

namespace OrderService.Storage;

public sealed class OrderStore
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();
    private readonly ConcurrentDictionary<string, Guid> _idempotencyKeyToOrderId = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _idempotencyKeyToRequestHash = new(StringComparer.Ordinal);

    public void Upsert(Order order) => _orders[order.Id] = order;

    public bool TryGet(Guid id, out Order? order) => _orders.TryGetValue(id, out order);

    public bool TryGetByIdempotencyKey(string idempotencyKey, string requestHash, out Order? order, out bool conflict)
    {
        conflict = false;
        order = null;

        if (!_idempotencyKeyToOrderId.TryGetValue(idempotencyKey, out var orderId))
        {
            return false;
        }

        if (_idempotencyKeyToRequestHash.TryGetValue(idempotencyKey, out var existingHash) &&
            !string.Equals(existingHash, requestHash, StringComparison.Ordinal))
        {
            conflict = true;
            return false;
        }

        return _orders.TryGetValue(orderId, out order);
    }

    public void SetIdempotencyKey(string idempotencyKey, string requestHash, Guid orderId)
    {
        _idempotencyKeyToOrderId[idempotencyKey] = orderId;
        _idempotencyKeyToRequestHash[idempotencyKey] = requestHash;
    }
}
