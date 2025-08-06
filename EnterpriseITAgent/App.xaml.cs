using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EnterpriseITAgent.Infrastructure;

namespace EnterpriseITAgent;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Create and configure the host
            _host = ServiceCollectionExtensions.CreateEnterpriseHostBuilder().Build();
            
            // Start the host
            await _host.StartAsync();
            
            // Get logger and log startup
            var logger = _host.Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Enterprise IT Agent starting up...");
            
            // Initialize core services
            await InitializeCoreServicesAsync();
            
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start application: {ex.Message}", "Startup Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host != null)
            {
                var logger = _host.Services.GetRequiredService<ILogger<App>>();
                logger.LogInformation("Enterprise IT Agent shutting down...");
                
                await _host.StopAsync();
                _host.Dispose();
            }
        }
        catch (Exception ex)
        {
            // Log error but don't prevent shutdown
            System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
        }
        
        base.OnExit(e);
    }

    private async Task InitializeCoreServicesAsync()
    {
        if (_host == null) return;

        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        
        try
        {
            // Initialize configuration manager
            var configManager = _host.Services.GetRequiredService<IConfigurationManager>();
            await configManager.LoadConfigurationAsync();
            configManager.WatchForConfigChanges();
            
            // Initialize network manager
            var networkManager = _host.Services.GetRequiredService<INetworkManager>();
            var networkStatus = await networkManager.GetNetworkStatusAsync();
            logger.LogInformation("Network status: Connected={IsConnected}", networkStatus.IsConnected);
            
            // Initialize logging service
            var loggingService = _host.Services.GetRequiredService<ILoggingService>();
            loggingService.LogInfo("Core services initialized successfully", "App");
            
            logger.LogInformation("Core services initialization completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize core services");
            throw;
        }
    }

    /// <summary>
    /// Gets a service from the dependency injection container
    /// </summary>
    /// <typeparam name="T">Service type</typeparam>
    /// <returns>Service instance</returns>
    public T? GetService<T>() where T : class
    {
        return _host?.Services.GetService<T>();
    }

    /// <summary>
    /// Gets a required service from the dependency injection container
    /// </summary>
    /// <typeparam name="T">Service type</typeparam>
    /// <returns>Service instance</returns>
    public T GetRequiredService<T>() where T : class
    {
        if (_host == null)
            throw new InvalidOperationException("Host not initialized");
            
        return _host.Services.GetRequiredService<T>();
    }
}

