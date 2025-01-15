using ApiGateway.Caching;
using ApiGateway.Extensions;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.RateLimiting;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IMemoryCacheService, MemoryCacheService>();
builder.Services.AddSingleton(_ =>
{
    var smtpUri = new Uri(builder.Configuration.GetConnectionString("SmtpUri") 
        ?? throw new InvalidDataException("SmtpUri not present in configuration!"));
    var smtpClient = new SmtpClient(smtpUri.Host, smtpUri.Port);
    return smtpClient;
});
builder.Services
    .AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
    .AddBearerToken();
builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy("LimitedAccessPolicy", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("limited-access", true.ToString()));
builder.Services.AddRateLimiter(
    options => options
        .AddFixedWindowLimiter("RateLimiterPolicy", opt =>
        {
            opt.PermitLimit = 5;
            opt.Window = TimeSpan.FromSeconds(20);
            opt.QueueLimit = 3;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        })
        .RejectionStatusCode = StatusCodes.Status429TooManyRequests
);
builder.Services.AddReverseProxyPipeline();

var app = builder.Build();

app.MapGet("login", (bool isLimitedAccess = false) =>
    Results.SignIn(
        new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("limited-access", isLimitedAccess.ToString())
            ], 
            BearerTokenDefaults.AuthenticationScheme)
        ), 
        authenticationScheme: BearerTokenDefaults.AuthenticationScheme));

app.MapPost("/mail", async (SmtpClient smtpClient, HttpContext context) =>
{
    var emailMessageData = await context.Request.ReadFromJsonAsync<EmailMessageData>() 
        ?? throw new InvalidDataException("Invalid email message data!");
    using var message = new MailMessage("newsletter@acme.com", emailMessageData.Email)
    {
        Subject = emailMessageData.Title,
        Body = emailMessageData.Content
    };
    await smtpClient.SendMailAsync(message);
});

app.UseRateLimiter();
app.MapReverseProxyPipeline();

app.Run();

record EmailMessageData(string Email, string Title, string Content);