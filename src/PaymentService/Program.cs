using PaymentService.Storage;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<PaymentStore>();

var app = builder.Build();

app.MapOpenApi();
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Redirect("/openapi/v1.json"))
    .ExcludeFromDescription();

app.MapPost("/payments", (HttpRequest httpRequest, PaymentRequest request, PaymentStore store) =>
    {
        if (!httpRequest.Headers.TryGetValue("Idempotency-Key", out var keyValues) ||
            string.IsNullOrWhiteSpace(keyValues.ToString()))
        {
            return Results.BadRequest(new { error = "Missing Idempotency-Key header." });
        }

        var now = DateTimeOffset.UtcNow;
        Payment payment;
        bool created;
        try
        {
            (payment, created) = store.GetOrCreate(keyValues.ToString(), request, now);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }

        return created
            ? Results.Created($"/payments/{payment.Id}", payment)
            : Results.Ok(payment);
    })
    .WithName("CreatePayment");

app.MapGet("/payments/{id:guid}", (Guid id, PaymentStore store) =>
    store.TryGet(id, out var payment) ? Results.Ok(payment) : Results.NotFound())
    .WithName("GetPaymentById");

app.Run();
