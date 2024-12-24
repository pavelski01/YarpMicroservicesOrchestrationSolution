using ApiGateway.Caching;
using ApiGateway.Configuration;
using ApiGateway.Services;
using System.Net.Mime;
using Yarp.ReverseProxy.Transforms;

namespace ApiGateway.Extensions;

public static class ReverseProxyExtensions
{
    public static IServiceCollection AddReverseProxyPipeline(this IServiceCollection services, IConfiguration? configuration = default)
    {
        var proxyBuilder = services.AddReverseProxy();
        if (configuration is null)
        {
            proxyBuilder.LoadFromMemory(BasicConfiguration.GetRoutes(), BasicConfiguration.GetClusters());
        }
        else
        {
            proxyBuilder.LoadFromConfig(configuration.GetSection("ApiGateway"));
        }
        proxyBuilder.AddTransforms(builder =>
        {
            builder.AddRequestTransform(transformContext =>
            {
                transformContext.HttpContext.Request.Headers["X-Modifier-Header"] = "skycore";
                transformContext.ProxyRequest.Headers.Add("X-Forwarded-Path", "/new-path");
                //transformContext.HttpContext.Request.Path = "/new-path";
                var request = transformContext.HttpContext.Request;
                Console.WriteLine($"Request Path: {request.Path}");
                return ValueTask.CompletedTask;
            });
            builder.AddResponseTransform(transformContext =>
            {
                transformContext.HttpContext.Response.Headers["X-Custom-Response-Header"] = "Custom Header included";
                Console.WriteLine($"Response is modified");
                return ValueTask.CompletedTask;
            });
        });

        return services;
    }

    public static IEndpointRouteBuilder MapReverseProxyPipeline(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapReverseProxy(proxyPipeline =>
            {
                proxyPipeline.UseSessionAffinity();
                proxyPipeline.UseLoadBalancing();
                proxyPipeline.UsePassiveHealthChecks();

                proxyPipeline.UseMiddleware<Interceptor>();
                proxyPipeline.UseReverseProxyPipeline();
            });
        return endpoints;
    }

    private static IApplicationBuilder UseReverseProxyPipeline(this IApplicationBuilder application)
    {
        application.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var cacheService = context.RequestServices.GetRequiredService<IMemoryCacheService>();
                var cachedResponse = cacheService.GetCache(context.Request.Path);
                if (!string.IsNullOrEmpty(cachedResponse))
                {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = MediaTypeNames.Application.Json;
                    await context.Response.WriteAsync(cachedResponse);
                    await context.Response.CompleteAsync();
                }
                else
                {
                    var originalResponseBodyStream = context.Response.Body;
                    await using var responseBody = new MemoryStream();
                    context.Response.Body = responseBody;

                    await next(context);

                    if (context.Response.StatusCode == StatusCodes.Status200OK)
                    {
                        context.Response.Body.Seek(0, SeekOrigin.Begin);
                        var responseBodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
                        var cacheKey = context.Request.Path.ToString();
                        cacheService.SetCache(cacheKey, responseBodyText, 60);

                        context.Response.Body.Seek(0, SeekOrigin.Begin);
                        await responseBody.CopyToAsync(originalResponseBodyStream);
                    }
                    context.Response.Body = originalResponseBodyStream;
                }
            }
            else
            {
                await next(context);
            }
        });
        return application;
    }
}
