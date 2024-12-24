namespace ApiGateway.Services
{
    public class Interceptor(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var log = string.Join("; ", context.Response.Headers.Select(pair => $"{pair.Key}:{pair.Value}"));
            Console.WriteLine(log);
            
            await next(context);
            
            log = string.Join("; ", context.Request.Headers.Select(pair => $"{pair.Key}:{pair.Value}"));
            Console.WriteLine(log);
        }
    }
}
