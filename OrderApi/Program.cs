using OrderApi;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/api/order", async () =>
{
    await Task.Delay(4000);
    var orders = new List<Order>();
    orders.AddRange([
        new(1, "Alice", "Laptop"),
        new(2, "Bob", "Smartphone"),
        new(3, "Charlie", "Tablet"),
        new(4, "Diana", "Smartwatch")
    ]);
    return Results.Json(orders);
});

app.Run();
