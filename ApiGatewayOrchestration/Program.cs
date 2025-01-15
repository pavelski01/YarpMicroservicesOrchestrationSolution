using ApiGatewayOrchestration.MailDev;

var builder = DistributedApplication.CreateBuilder(args);

var orderApi = builder.AddProject<Projects.OrderApi>("OrderApi")
    .WithExternalHttpEndpoints();
var productApi = builder.AddProject<Projects.ProductApi>("ProductApi")
    .WithExternalHttpEndpoints();
var productApi2 = builder.AddProject<Projects.ProductApi>("ProductApi2", null as string)
    .WithEndpoint(5003, null, "http")
    .WithReplicas(2);
var smtp = builder.AddMailDev("SmtpUri");
var _ = builder.AddProject<Projects.ApiGateway>("ApiGateway")
    .WithReference(smtp)
    .WithExternalHttpEndpoints()
    .WaitFor(smtp)
    .WaitFor(orderApi)
    .WaitFor(productApi)
    .WaitFor(productApi2);

builder.Build().Run();
