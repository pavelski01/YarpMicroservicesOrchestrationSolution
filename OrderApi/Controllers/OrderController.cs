using Microsoft.AspNetCore.Mvc;

namespace OrderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<Order>> Get()
    {
        //await Task.Delay(4000);
        var orders = new List<Order>();
        orders.AddRange([
            new(1, "Alice", "Laptop"),
            new(2, "Bob", "Smartphone"),
            new(3, "Charlie", "Tablet"),
            new(4, "Diana", "Smartwatch"),
        ]);
        return orders;
    }
}
