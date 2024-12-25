var builder = DistributedApplication.CreateBuilder(args);

var orderApi = builder.AddProject<Projects.OrderApi>("OrderApi")
    .WithExternalHttpEndpoints();
var productApi = builder.AddProject<Projects.ProductApi>("ProductApi")
    .WithExternalHttpEndpoints();
var productApi2 = builder.AddProject<Projects.ProductApi>("ProductApi2", null as string)
    .WithEndpoint(5003, null, "http");
var productApi3 = builder.AddProject<Projects.ProductApi>("ProductApi3", null as string)
    .WithEndpoint(5004, null, "http");
var _ = builder.AddProject<Projects.ApiGateway>("ApiGateway")
    .WithExternalHttpEndpoints()
    .WaitFor(orderApi)
    .WaitFor(productApi)
    .WaitFor(productApi2)
    .WaitFor(productApi3);

builder.Build().Run();
