using OrderService.Clients;
using OrderService.Storage;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<OrderStore>();

builder.Services.AddHttpClient<CatalogClient>(httpClient =>
{
    var baseUrl = builder.Configuration["CatalogService:BaseUrl"] ?? "http://localhost:8081/";
    httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
});

builder.Services.AddHttpClient<PaymentClient>(httpClient =>
{
    var baseUrl = builder.Configuration["PaymentService:BaseUrl"] ?? "http://localhost:8082/";
    httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
});

var app = builder.Build();

app.MapOpenApi();
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Redirect("/openapi/v1.json"))
    .ExcludeFromDescription();

app.MapPost("/orders", async (
        HttpRequest httpRequest,
        CreateOrderRequest request,
        CatalogClient catalogClient,
        PaymentClient paymentClient,
        OrderStore store,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
    {
        var logger = loggerFactory.CreateLogger("OrderService");

        if (request.ItemIds is null || request.ItemIds.Count == 0)
        {
            return Results.BadRequest(new { error = "At least one itemId is required." });
        }

        var lines = new List<OrderLine>(request.ItemIds.Count);
        foreach (var itemId in request.ItemIds)
        {
            var item = await catalogClient.GetItem(itemId, cancellationToken);
            if (item is null)
            {
                return Results.BadRequest(new { error = $"Unknown itemId: {itemId}" });
            }

            lines.Add(new OrderLine(item.Id, item.Name, item.Price));
        }

        var total = lines.Sum(l => l.UnitPrice);
        var orderId = Guid.NewGuid();

        var idempotencyKey = httpRequest.Headers.TryGetValue("Idempotency-Key", out var keyValues) &&
                             !string.IsNullOrWhiteSpace(keyValues.ToString())
            ? keyValues.ToString()
            : $"order-{orderId}";

        Payment payment;
        try
        {
            payment = await paymentClient.Authorize(new PaymentRequest(orderId, total), idempotencyKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payment authorization failed for order {OrderId}", orderId);
            var failed = new Order(orderId, lines, total, null, OrderStatus.Failed, DateTimeOffset.UtcNow);
            store.Upsert(failed);
            return Results.Problem("Payment authorization failed.", statusCode: StatusCodes.Status502BadGateway);
        }

        var status = payment.Status == PaymentStatus.Authorized ? OrderStatus.PaymentAuthorized : OrderStatus.Failed;
        var order = new Order(
            orderId,
            lines,
            total,
            new OrderPayment(payment.Id, payment.Status),
            status,
            DateTimeOffset.UtcNow);

        store.Upsert(order);
        return Results.Created($"/orders/{order.Id}", order);
    })
    .WithName("CreateOrder");

app.MapGet("/orders/{id:guid}", (Guid id, OrderStore store) =>
    store.TryGet(id, out var order) ? Results.Ok(order) : Results.NotFound())
    .WithName("GetOrderById");

app.Run();
