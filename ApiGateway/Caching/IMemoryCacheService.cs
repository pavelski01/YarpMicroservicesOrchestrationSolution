namespace ApiGateway.Caching;

public interface IMemoryCacheService
{
    void SetCache(string key, object value, int expirationInSeconds);
    string? GetCache(string key);
}