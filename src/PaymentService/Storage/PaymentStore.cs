using System.Collections.Concurrent;
using Shared.Contracts;

namespace PaymentService.Storage;

public sealed class PaymentStore
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();
    private readonly ConcurrentDictionary<string, Guid> _idempotencyKeyToPaymentId = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PaymentRequest> _idempotencyKeyToRequest = new(StringComparer.Ordinal);

    public bool TryGet(Guid id, out Payment? payment) => _payments.TryGetValue(id, out payment);

    public (Payment Payment, bool Created) GetOrCreate(string idempotencyKey, PaymentRequest request, DateTimeOffset now)
    {
        if (_idempotencyKeyToPaymentId.TryGetValue(idempotencyKey, out var existingId) &&
            _payments.TryGetValue(existingId, out var existing))
        {
            if (_idempotencyKeyToRequest.TryGetValue(idempotencyKey, out var existingRequest) &&
                existingRequest != request)
            {
                throw new InvalidOperationException("Idempotency-Key has already been used with a different request.");
            }

            return (existing, false);
        }

        var status = request.Amount > 0 ? PaymentStatus.Authorized : PaymentStatus.Declined;
        var created = new Payment(Guid.NewGuid(), request.OrderId, request.Amount, status, now);
        _payments[created.Id] = created;
        _idempotencyKeyToPaymentId[idempotencyKey] = created.Id;
        _idempotencyKeyToRequest[idempotencyKey] = request;
        return (created, true);
    }
}
