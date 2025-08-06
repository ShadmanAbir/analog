using EnterpriseITAgent.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);

// Add SQLite database
var database = builder.AddSqlite("enterprisedb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Add the main Enterprise IT Agent service
var enterpriseAgent = builder.AddProject<Projects.EnterpriseITAgent>("enterprise-agent")
    .WithReference(database)
    .WithReplicas(1);

// Add health checks dashboard
builder.AddProject<Projects.EnterpriseITAgent_HealthChecks>("health-dashboard")
    .WithReference(database)
    .WithHttpEndpoint(port: 5001, name: "health-ui");

// Add monitoring and observability
builder.Services.AddServiceDefaults();

builder.Build().Run();