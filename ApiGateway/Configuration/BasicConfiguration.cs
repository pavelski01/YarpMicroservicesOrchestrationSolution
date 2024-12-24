using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.LoadBalancing;

namespace ApiGateway.Configuration;

public static class BasicConfiguration
{
    public static IReadOnlyList<RouteConfig> GetRoutes()
    {
        return
        [
            new()
            {
                RouteId = "product-route",
                //RateLimiterPolicy = "RateLimiterPolicy",
                AuthorizationPolicy = "Default",
                ClusterId = "product-cluster",
                Match = new()
                {
                    Path = "/api/product/{*any}"
                }
            },
            new()
            {
                RouteId = "order-route",
                ClusterId = "order-cluster",
                //RateLimiterPolicy = "RateLimiterPolicy",
                AuthorizationPolicy = "myPolicy",
                Match = new()
                {
                    Path = "/api/order/{*any}"
                }
            }
        ];
    }

    public static IReadOnlyList<ClusterConfig> GetClusters()
    {
        return
        [
            new()
            {
                ClusterId = "product-cluster",
                //LoadBalancingPolicy = LoadBalancingPolicies.RoundRobin,
                Destinations = new Dictionary<string, DestinationConfig>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { "product-destination1", new() { Address = "http://localhost:5001" } },
                    //{ "product-destination2", new() { Address = "http://localhost:5003" } },
                    //{ "product-destination3", new() { Address = "http://localhost:5004" } }
                }
            },
            new()
            {
                ClusterId = "order-cluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "order-destination", new() { Address = "http://localhost:5002" } }
                }
            }
        ];
    }
}