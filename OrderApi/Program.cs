using OrderApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
List<Order> orders = [
    new(1, "Alice", "Laptop"),
    new(2, "Bob", "Smartphone"),
    new(3, "Charlie", "Tablet"),
    new(4, "Diana", "Smartwatch")
];
app.MapGet("/api/order", async () =>
{
    Console.WriteLine($"Endpoint called: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm}");
    await Task.Delay(4000);
    return Results.Json(orders);
});

app.Run();
