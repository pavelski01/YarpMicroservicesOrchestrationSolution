using ApiGateway.Caching;
using ApiGateway.Extensions;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IMemoryCacheService, MemoryCacheService>();

builder.Services.AddRateLimiter(
    options => options.AddFixedWindowLimiter("RateLimiterPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromSeconds(20);
        //opt.QueueLimit = 2;
        //opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    }).RejectionStatusCode = StatusCodes.Status429TooManyRequests
);
builder.Services.AddReverseProxyPipeline();

var app = builder.Build();

app.UseRateLimiter();
app.MapControllers();
app.MapReverseProxyPipeline();

app.Run();