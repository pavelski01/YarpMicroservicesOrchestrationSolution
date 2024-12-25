var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/api/product", async () =>
{
    await Task.Delay(4000);
    string[] products = ["Clothes", "Bags", "Televisions", "Shoes", "Phones", "Accessories"];
    return Results.Json(products);
});

app.Run();
