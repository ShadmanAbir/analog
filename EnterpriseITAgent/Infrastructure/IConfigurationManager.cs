using System.Threading.Tasks;
using EnterpriseITAgent.Models;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// Interface for managing application configuration from local files and central API
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Loads configuration from local file or central API
    /// </summary>
    /// <returns>The loaded configuration</returns>
    Task<Configuration> LoadConfigurationAsync();

    /// <summary>
    /// Applies the given configuration to the system
    /// </summary>
    /// <param name="config">Configuration to apply</param>
    Task ApplyConfigurationAsync(Configuration config);

    /// <summary>
    /// Attempts to fetch configuration from the ERP system
    /// </summary>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> TryFetchFromErpAsync();

    /// <summary>
    /// Starts watching for configuration file changes
    /// </summary>
    void WatchForConfigChanges();

    /// <summary>
    /// Saves configuration to local file
    /// </summary>
    /// <param name="config">Configuration to save</param>
    Task SaveConfigurationAsync(Configuration config);
}