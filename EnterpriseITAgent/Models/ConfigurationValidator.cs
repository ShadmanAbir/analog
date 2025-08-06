using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EnterpriseITAgent.Models;

/// <summary>
/// Provides validation functionality for configuration models
/// </summary>
public static class ConfigurationValidator
{
    /// <summary>
    /// Validates a configuration object and returns validation results
    /// </summary>
    /// <param name="configuration">The configuration to validate</param>
    /// <returns>Collection of validation results</returns>
    public static IEnumerable<ValidationResult> ValidateConfiguration(Configuration configuration)
    {
        var context = new ValidationContext(configuration);
        var results = new List<ValidationResult>();
        
        // Validate the main configuration object
        Validator.TryValidateObject(configuration, context, results, true);
        
        // Validate nested objects
        if (configuration.Email != null)
        {
            var emailResults = ValidateEmailConfiguration(configuration.Email);
            results.AddRange(emailResults.Select(r => new ValidationResult(
                $"Email.{r.ErrorMessage}", 
                r.MemberNames?.Select(m => $"Email.{m}"))));
        }
        
        if (configuration.Printers != null)
        {
            for (int i = 0; i < configuration.Printers.Length; i++)
            {
                var printerResults = ValidatePrinterConfiguration(configuration.Printers[i]);
                results.AddRange(printerResults.Select(r => new ValidationResult(
                    $"Printers[{i}].{r.ErrorMessage}", 
                    r.MemberNames?.Select(m => $"Printers[{i}].{m}"))));
            }
        }
        
        if (configuration.Backup != null)
        {
            var backupResults = ValidateBackupConfiguration(configuration.Backup);
            results.AddRange(backupResults.Select(r => new ValidationResult(
                $"Backup.{r.ErrorMessage}", 
                r.MemberNames?.Select(m => $"Backup.{m}"))));
        }
        
        if (configuration.Security != null)
        {
            var securityResults = ValidateSecurityConfiguration(configuration.Security);
            results.AddRange(securityResults.Select(r => new ValidationResult(
                $"Security.{r.ErrorMessage}", 
                r.MemberNames?.Select(m => $"Security.{m}"))));
        }
        
        return results;
    }
    
    /// <summary>
    /// Validates an email configuration object
    /// </summary>
    /// <param name="emailConfig">The email configuration to validate</param>
    /// <returns>Collection of validation results</returns>
    public static IEnumerable<ValidationResult> ValidateEmailConfiguration(EmailConfiguration emailConfig)
    {
        var context = new ValidationContext(emailConfig);
        var results = new List<ValidationResult>();
        
        Validator.TryValidateObject(emailConfig, context, results, true);
        
        return results;
    }
    
    /// <summary>
    /// Validates a printer configuration object
    /// </summary>
    /// <param name="printerConfig">The printer configuration to validate</param>
    /// <returns>Collection of validation results</returns>
    public static IEnumerable<ValidationResult> ValidatePrinterConfiguration(PrinterConfiguration printerConfig)
    {
        var context = new ValidationContext(printerConfig);
        var results = new List<ValidationResult>();
        
        Validator.TryValidateObject(printerConfig, context, results, true);
        
        return results;
    }
    
    /// <summary>
    /// Validates a backup configuration object
    /// </summary>
    /// <param name="backupConfig">The backup configuration to validate</param>
    /// <returns>Collection of validation results</returns>
    public static IEnumerable<ValidationResult> ValidateBackupConfiguration(BackupConfiguration backupConfig)
    {
        var context = new ValidationContext(backupConfig);
        var results = new List<ValidationResult>();
        
        Validator.TryValidateObject(backupConfig, context, results, true);
        
        // Additional custom validation for backup paths
        if (backupConfig.BackupPaths != null)
        {
            for (int i = 0; i < backupConfig.BackupPaths.Length; i++)
            {
                var path = backupConfig.BackupPaths[i];
                if (string.IsNullOrWhiteSpace(path))
                {
                    results.Add(new ValidationResult(
                        $"Backup path at index {i} cannot be empty", 
                        new[] { $"BackupPaths[{i}]" }));
                }
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Validates a security configuration object
    /// </summary>
    /// <param name="securityConfig">The security configuration to validate</param>
    /// <returns>Collection of validation results</returns>
    public static IEnumerable<ValidationResult> ValidateSecurityConfiguration(SecurityConfiguration securityConfig)
    {
        var context = new ValidationContext(securityConfig);
        var results = new List<ValidationResult>();
        
        Validator.TryValidateObject(securityConfig, context, results, true);
        
        return results;
    }
    
    /// <summary>
    /// Checks if a configuration object is valid
    /// </summary>
    /// <param name="configuration">The configuration to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(Configuration configuration)
    {
        var results = ValidateConfiguration(configuration);
        return !results.Any();
    }
    
    /// <summary>
    /// Gets validation error messages for a configuration object
    /// </summary>
    /// <param name="configuration">The configuration to validate</param>
    /// <returns>List of error messages</returns>
    public static List<string> GetValidationErrors(Configuration configuration)
    {
        var results = ValidateConfiguration(configuration);
        return results.Select(r => r.ErrorMessage ?? "Unknown validation error").ToList();
    }
}