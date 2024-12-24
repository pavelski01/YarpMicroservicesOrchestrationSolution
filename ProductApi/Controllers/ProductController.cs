using Microsoft.AspNetCore.Mvc;

namespace productApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private static readonly string[] Products =
    [
        "Clothes", "Bags", "Televisions", "Shoes", "Phones", "Accessories"
    ];

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        //await Task.Delay(4000);
        return Ok(Products);
    }
}

