using ApiGateway.Caching;
using ApiGateway.Configuration;
using ApiGateway.Data;
using ApiGateway.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IMemoryCacheService, MemoryCacheService>();
builder.Services.AddDbContext<AppIdentityDbContext>(o => o.UseSqlite("Data Source=DemoDb.db"));
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppIdentityDbContext>()
    .AddRoles<IdentityRole>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAnyOrigin",
        builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddHttpLogging(logging => logging.LoggingFields = HttpLoggingFields.All);

//builder.Services
//    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
//{
//    var key = Encoding.UTF8.GetBytes(builder.Configuration.GetSection("Authentication:Key").Value!);
//    var issuer = builder.Configuration.GetSection("Authentication:Issuer").Value!;
//    var audience = builder.Configuration.GetSection("Authentication:Audience").Value!;

//    options.RequireHttpsMetadata = false;
//    options.SaveToken = true;
//    options.TokenValidationParameters = new()
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = false,
//        ValidIssuer = issuer,
//        ValidAudience = audience,
//        IssuerSigningKey = new SymmetricSecurityKey(key),
//        ClockSkew = TimeSpan.Zero
//    };
//    options.Events = new JwtBearerEvents
//    {
//        OnMessageReceived = context =>
//        {
//            context.Token = context.Request.Headers.Authorization[0]!.Split(' ')[1];
//            return Task.CompletedTask;
//        }
//    };
//});
builder.Services
    .AddReverseProxy()
    .LoadFromMemory(BasicConfiguration.GetRoutes(), BasicConfiguration.GetClusters())
    .AddTransforms(transformBuilderContext =>
    {
        if (string.Equals("myPolicy", transformBuilderContext.Route.AuthorizationPolicy))
        {
            transformBuilderContext.AddRequestTransform(async transformContext =>
            {
                if (transformContext.HttpContext.User.Identity.IsAuthenticated)
                {
                    // Extract the JWT token from the incoming request
                    var token = await transformContext.HttpContext.GetTokenAsync("access_token");

                    // Add the token to the outgoing request headers
                    if (!string.IsNullOrEmpty(token))
                    {
                        transformContext.ProxyRequest.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                }
            });
        }
    });

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.IncludeErrorDetails = true;
        options.RequireHttpsMetadata = false;
        options.Authority = "http://localhost:3000";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization(options => options.AddPolicy("myPolicy", builder => builder
        .RequireClaim("myCustomClaim", "green")
        .RequireAuthenticatedUser()));


//const string DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//}).AddJwtBearer(DefaultScheme, options =>
//{
//    var key = Encoding.UTF8.GetBytes(builder.Configuration.GetSection("Authentication:Key").Value!);
//    var issuer = builder.Configuration.GetSection("Authentication:Issuer").Value!;
//    var audience = builder.Configuration.GetSection("Authentication:Audience").Value!;

//    options.RequireHttpsMetadata = false;
//    options.SaveToken = true;
//    //options.TokenValidationParameters = new()
//    //{
//    //    ValidateIssuer = false,
//    //    ValidateAudience = false,
//    //    ValidateLifetime = false,
//    //    ValidIssuer = issuer,
//    //    ValidAudience = audience,
//    //    IssuerSigningKey = new SymmetricSecurityKey(key)
//    //};
//});

#if TEST
builder.Services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
    .AddBearerToken();

builder.Services.AddAuthorization(o =>
    o.AddPolicy(
        "is-vip",
        b => b
            .RequireAuthenticatedUser()
            .RequireClaim("vip", true.ToString())));
#endif

//builder.Services.AddAuthorization(o =>
//{
//    o.AddPolicy("is-vip", policy => policy.RequireAuthenticatedUser());
//    //o.AddPolicy("UserPolicy", policy => policy.RequireRole("User"));
//});
//builder.Services
//    .AddAuthorizationBuilder();


//builder.Services.AddRateLimiter(
//    options => options.AddFixedWindowLimiter("RateLimiterPolicy", opt =>
//    {
//        opt.PermitLimit = 1;
//        opt.Window = TimeSpan.FromSeconds(20);
//        //opt.QueueLimit = 2;
//        //opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
//    }).RejectionStatusCode = StatusCodes.Status429TooManyRequests
//);
//builder.Services.AddReverseProxyPipeline();


var app = builder.Build();

//app.MapGet("api/login", (bool isVip = true) =>
//    Results.SignIn(
//        new ClaimsPrincipal(
//            new ClaimsIdentity(
//                [
//                    new Claim("sub", Guid.NewGuid().ToString()),
//                    new Claim("vip", isVip.ToString())
//                ],
//                BearerTokenDefaults.AuthenticationScheme
//            )
//        ),
//        authenticationScheme: BearerTokenDefaults.AuthenticationScheme));

app.UseHttpLogging();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
//app.UseRateLimiter();
app.MapControllers();
app.UseCors("AllowAnyOrigin");
app.MapReverseProxy();
//app.MapReverseProxyPipeline();


//app.Use(async (context, next) =>
//{
//    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//    if (string.IsNullOrEmpty(token))
//    {
//        context.Response.StatusCode = 401;
//        await context.Response.WriteAsync("Authorization header missing.");
//        return;
//    }

//    try
//    {
//        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
//        var jwtSecurityToken = jwtSecurityTokenHandler.ReadJwtToken(token);

//        // Determine the API (audience) based on the request, and set issuer and audience values accordingly
//        var validIssuer = jwtSecurityToken.Issuer;
//        var validAudience = jwtSecurityToken.Audiences.FirstOrDefault(); // or set based on the API

//        var validationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidIssuer = validIssuer,
//            ValidAudience = validAudience,
//            ValidateIssuerSigningKey = true,
//            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("B692AEC4839A483E814E55720ED1BB47")), // Normally, you'd fetch this dynamically from Azure AD's jwks endpoint.
//            ValidateLifetime = false // This checks the expiry
//        };

//        // This will throw if invalid
//        jwtSecurityTokenHandler.ValidateToken(token, validationParameters, out _);
//    }
//    catch
//    {
//        context.Response.StatusCode = 401;
//        await context.Response.WriteAsync("Invalid token.");
//        return;
//    }

//    await next.Invoke();
//});

app.Run();