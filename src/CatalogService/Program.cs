using CatalogService.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<CatalogStore>();

var app = builder.Build();

app.MapOpenApi();
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Redirect("/openapi/v1.json"))
    .ExcludeFromDescription();

app.MapGet("/items", (CatalogStore store) => Results.Ok(store.GetAll()))
    .WithName("GetItems");

app.MapGet("/items/{id:guid}", (Guid id, CatalogStore store) =>
    store.TryGet(id, out var item) ? Results.Ok(item) : Results.NotFound())
    .WithName("GetItemById");

app.Run();
