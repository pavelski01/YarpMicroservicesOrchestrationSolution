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
string[] products = ["Clothes", "Bags", "Televisions", "Shoes", "Phones", "Accessories"];
app.MapGet("/api/product", async () =>
{
    Console.WriteLine($"Endpoint called: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm}");
    await Task.Delay(4000);
    return Results.Json(products);
});

app.Run();
