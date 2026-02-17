namespace Shared.Contracts;

public sealed record CatalogItem(Guid Id, string Name, decimal Price);

public sealed record PaymentRequest(Guid OrderId, decimal Amount);

public enum PaymentStatus
{
    Authorized = 0,
    Declined = 1,
}

public sealed record Payment(Guid Id, Guid OrderId, decimal Amount, PaymentStatus Status, DateTimeOffset CreatedAt);

public sealed record CreateOrderRequest(IReadOnlyList<Guid> ItemIds);

public enum OrderStatus
{
    Created = 0,
    PaymentAuthorized = 1,
    Failed = 2,
}

public sealed record OrderLine(Guid ItemId, string Name, decimal UnitPrice);

public sealed record OrderPayment(Guid PaymentId, PaymentStatus Status);

public sealed record Order(
    Guid Id,
    IReadOnlyList<OrderLine> Lines,
    decimal TotalAmount,
    OrderPayment? Payment,
    OrderStatus Status,
    DateTimeOffset CreatedAt);

