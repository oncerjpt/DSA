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

        if (!httpRequest.Headers.TryGetValue("Idempotency-Key", out var keyValues) ||
            string.IsNullOrWhiteSpace(keyValues.ToString()))
        {
            return Results.BadRequest(new { error = "Missing Idempotency-Key header." });
        }

        var idempotencyKey = keyValues.ToString();
        var requestHash = string.Join(",", request.ItemIds.OrderBy(i => i).Select(i => i.ToString("N")));
        if (store.TryGetByIdempotencyKey(idempotencyKey, requestHash, out var existingOrder, out var conflict))
        {
            return Results.Ok(existingOrder);
        }

        if (conflict)
        {
            return Results.Problem("Idempotency-Key has already been used with a different request.", statusCode: StatusCodes.Status409Conflict);
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
            store.SetIdempotencyKey(idempotencyKey, requestHash, orderId);
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
        store.SetIdempotencyKey(idempotencyKey, requestHash, orderId);
        return Results.Created($"/orders/{order.Id}", order);
    })
    .WithName("CreateOrder");

app.MapGet("/orders/{id:guid}", (Guid id, OrderStore store) =>
    store.TryGet(id, out var order) ? Results.Ok(order) : Results.NotFound())
    .WithName("GetOrderById");

app.Run();
