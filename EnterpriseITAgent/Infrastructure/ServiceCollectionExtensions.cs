using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// Extension methods for configuring dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures all core services for the Enterprise IT Agent
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection ConfigureEnterpriseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        // Register core infrastructure services
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<INetworkManager, NetworkManager>();
        services.AddSingleton<ISystemMetricsCollector, SystemMetricsCollector>();

        // Register configuration
        services.AddSingleton(configuration);

        return services;
    }

    /// <summary>
    /// Configures the application host with all necessary services
    /// </summary>
    /// <returns>Configured host builder</returns>
    public static IHostBuilder CreateEnterpriseHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile("userconfig.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                services.ConfigureEnterpriseServices(context.Configuration);
            });
    }
}