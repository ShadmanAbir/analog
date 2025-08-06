using EnterpriseITAgent.ServiceDefaults;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add Health Checks UI
builder.Services.AddHealthChecksUI(opt =>
{
    opt.SetEvaluationTimeInSeconds(15); // time in seconds between check
    opt.MaximumHistoryEntriesPerEndpoint(60); // maximum history of checks
    opt.SetApiMaxActiveRequests(1); // api requests concurrency

    opt.AddHealthCheckEndpoint("Enterprise IT Agent", "/health");
})
.AddInMemoryStorage();

// Add health checks for dependencies
builder.Services.AddHealthChecks()
    .AddSqlite(builder.Configuration.GetConnectionString("enterprisedb") ?? "Data Source=enterprise.db");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

app.MapHealthChecks("/health", new HealthCheckOptions()
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui"; // this is ui path in your browser
    options.ApiPath = "/health-ui-api"; // the UI (spa app) use this path to get information from the store
});

app.MapDefaultEndpoints();

app.Run();