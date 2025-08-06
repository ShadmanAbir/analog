using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// Cryptographic service providing AES-256 encryption and secure key management
/// </summary>
public class CryptoService : ICryptoService, IDisposable
{
    private readonly ILogger<CryptoService> _logger;
    private readonly ILoggingService _loggingService;
    private readonly Dictionary<string, byte[]> _keyCache;
    private readonly object _keyCacheLock = new();
    private readonly string _credentialTarget = "EnterpriseITAgent";
    private bool _disposed;

    public CryptoService(ILogger<CryptoService> logger, ILoggingService loggingService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        _keyCache = new Dictionary<string, byte[]>();
        
        _loggingService.LogInfo("CryptoService initialized with AES-256 encryption", "CryptoService");
    }

    public async Task<string> EncryptAsync(string plainText, string? keyIdentifier = null)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));

        var data = Encoding.UTF8.GetBytes(plainText);
        var encryptedData = await EncryptAsync(data, keyIdentifier);
        return Convert.ToBase64String(encryptedData);
    }

    public async Task<byte[]> EncryptAsync(byte[] data, string? keyIdentifier = null)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        try
        {
            var key = await GetOrCreateKeyAsync(keyIdentifier ?? "default");
            
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            
            // Write IV to the beginning of the encrypted data
            await msEncrypt.WriteAsync(aes.IV, 0, aes.IV.Length);
            
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                await csEncrypt.WriteAsync(data, 0, data.Length);
                csEncrypt.FlushFinalBlock();
            }

            var result = msEncrypt.ToArray();
            _loggingService.LogDebug($"Successfully encrypted {data.Length} bytes", "CryptoService");
            return result;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, "Encryption failed", "CryptoService");
            throw;
        }
    }

    public async Task<string> DecryptAsync(string encryptedData, string? keyIdentifier = null)
    {
        if (string.IsNullOrEmpty(encryptedData))
            throw new ArgumentException("Encrypted data cannot be null or empty", nameof(encryptedData));

        var data = Convert.FromBase64String(encryptedData);
        var decryptedData = await DecryptAsync(data, keyIdentifier);
        return Encoding.UTF8.GetString(decryptedData);
    }

    public async Task<byte[]> DecryptAsync(byte[] encryptedData, string? keyIdentifier = null)
    {
        if (encryptedData == null || encryptedData.Length == 0)
            throw new ArgumentException("Encrypted data cannot be null or empty", nameof(encryptedData));

        try
        {
            var key = await GetOrCreateKeyAsync(keyIdentifier ?? "default");
            
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;

            // Extract IV from the beginning of the encrypted data
            var iv = new byte[aes.BlockSize / 8];
            Array.Copy(encryptedData, 0, iv, 0, iv.Length);
            aes.IV = iv;

            var cipherData = new byte[encryptedData.Length - iv.Length];
            Array.Copy(encryptedData, iv.Length, cipherData, 0, cipherData.Length);

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherData);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var msResult = new MemoryStream();
            
            await csDecrypt.CopyToAsync(msResult);
            var result = msResult.ToArray();
            
            _loggingService.LogDebug($"Successfully decrypted {result.Length} bytes", "CryptoService");
            return result;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, "Decryption failed", "CryptoService");
            throw;
        }
    }

    public string ComputeHash(byte[] data, string algorithm = "SHA256")
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        using var hashAlgorithm = CreateHashAlgorithm(algorithm);
        var hash = hashAlgorithm.ComputeHash(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string ComputeHash(string input, string algorithm = "SHA256")
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Input cannot be null or empty", nameof(input));

        var data = Encoding.UTF8.GetBytes(input);
        return ComputeHash(data, algorithm);
    }

    public bool VerifyHash(byte[] data, string hash, string algorithm = "SHA256")
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));
        if (string.IsNullOrEmpty(hash))
            throw new ArgumentException("Hash cannot be null or empty", nameof(hash));

        var computedHash = ComputeHash(data, algorithm);
        return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
    }

    public bool VerifyHash(string input, string hash, string algorithm = "SHA256")
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Input cannot be null or empty", nameof(input));
        if (string.IsNullOrEmpty(hash))
            throw new ArgumentException("Hash cannot be null or empty", nameof(hash));

        var computedHash = ComputeHash(input, algorithm);
        return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> DeriveNodeKeyAsync(string nodeId, string? additionalEntropy = null)
    {
        if (string.IsNullOrEmpty(nodeId))
            throw new ArgumentException("Node ID cannot be null or empty", nameof(nodeId));

        try
        {
            var machineIdentity = await GetMachineIdentityAsync();
            var keyIdentifier = $"node-{nodeId}";
            
            // Check if key already exists
            if (await KeyExistsAsync(keyIdentifier))
            {
                _loggingService.LogDebug($"Node key already exists for: {nodeId}", "CryptoService");
                return keyIdentifier;
            }

            // Derive key from machine identity
            var keyMaterial = $"{machineIdentity.IdentityHash}:{nodeId}:{additionalEntropy ?? ""}";
            var keyBytes = DeriveKeyFromPassword(keyMaterial, machineIdentity.MacAddress, 32);
            
            // Store the derived key securely
            await StoreKeySecurelyAsync(keyIdentifier, keyBytes);
            
            _loggingService.LogInfo($"Node key derived and stored for: {nodeId}", "CryptoService");
            return keyIdentifier;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, $"Failed to derive node key for: {nodeId}", "CryptoService");
            throw;
        }
    }

    public async Task<string> GenerateKeyAsync(string keyIdentifier, int keySize = 256)
    {
        if (string.IsNullOrEmpty(keyIdentifier))
            throw new ArgumentException("Key identifier cannot be null or empty", nameof(keyIdentifier));
        if (keySize != 128 && keySize != 192 && keySize != 256)
            throw new ArgumentException("Key size must be 128, 192, or 256 bits", nameof(keySize));

        try
        {
            var keyBytes = GenerateSecureRandomBytes(keySize / 8);
            await StoreKeySecurelyAsync(keyIdentifier, keyBytes);
            
            _loggingService.LogInfo($"Generated new {keySize}-bit key: {keyIdentifier}", "CryptoService");
            return keyIdentifier;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, $"Failed to generate key: {keyIdentifier}", "CryptoService");
            throw;
        }
    }

    public async Task<bool> StoreKeySecurelyAsync(string keyIdentifier, byte[] keyData)
    {
        if (string.IsNullOrEmpty(keyIdentifier))
            throw new ArgumentException("Key identifier cannot be null or empty", nameof(keyIdentifier));
        if (keyData == null || keyData.Length == 0)
            throw new ArgumentException("Key data cannot be null or empty", nameof(keyData));

        try
        {
            // Store in Windows Credential Manager
            var credentialName = $"{_credentialTarget}:{keyIdentifier}";
            var keyBase64 = Convert.ToBase64String(keyData);
            
            // Use Windows Credential Manager API
            var success = StoreCredential(credentialName, keyBase64);
            
            if (success)
            {
                // Also cache in memory for performance
                lock (_keyCacheLock)
                {
                    _keyCache[keyIdentifier] = (byte[])keyData.Clone();
                }
                
                _loggingService.LogDebug($"Key stored securely: {keyIdentifier}", "CryptoService");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, $"Failed to store key securely: {keyIdentifier}", "CryptoService");
            return false;
        }
    }

    public async Task<byte[]?> RetrieveKeySecurelyAsync(string keyIdentifier)
    {
        if (string.IsNullOrEmpty(keyIdentifier))
            throw new ArgumentException("Key identifier cannot be null or empty", nameof(keyIdentifier));

        try
        {
            // Check cache first
            lock (_keyCacheLock)
            {
                if (_keyCache.TryGetValue(keyIdentifier, out var cachedKey))
                {
                    return (byte[])cachedKey.Clone();
                }
            }

            // Retrieve from Windows Credential Manager
            var credentialName = $"{_credentialTarget}:{keyIdentifier}";
            var keyBase64 = RetrieveCredential(credentialName);
            
            if (!string.IsNullOrEmpty(keyBase64))
            {
                var keyData = Convert.FromBase64String(keyBase64);
                
                // Cache for future use
                lock (_keyCacheLock)
                {
                    _keyCache[keyIdentifier] = (byte[])keyData.Clone();
                }
                
                _loggingService.LogDebug($"Key retrieved securely: {keyIdentifier}", "CryptoService");
                return keyData;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, $"Failed to retrieve key securely: {keyIdentifier}", "CryptoService");
            return null;
        }
    }

    public async Task<bool> DeleteKeySecurelyAsync(string keyIdentifier)
    {
        if (string.IsNullOrEmpty(keyIdentifier))
            throw new ArgumentException("Key identifier cannot be null or empty", nameof(keyIdentifier));

        try
        {
            // Remove from cache
            lock (_keyCacheLock)
            {
                _keyCache.Remove(keyIdentifier);
            }

            // Delete from Windows Credential Manager
            var credentialName = $"{_credentialTarget}:{keyIdentifier}";
            var success = DeleteCredential(credentialName);
            
            if (success)
            {
                _loggingService.LogInfo($"Key deleted securely: {keyIdentifier}", "CryptoService");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, $"Failed to delete key securely: {keyIdentifier}", "CryptoService");
            return false;
        }
    }

    public async Task<bool> KeyExistsAsync(string keyIdentifier)
    {
        if (string.IsNullOrEmpty(keyIdentifier))
            throw new ArgumentException("Key identifier cannot be null or empty", nameof(keyIdentifier));

        // Check cache first
        lock (_keyCacheLock)
        {
            if (_keyCache.ContainsKey(keyIdentifier))
            {
                return true;
            }
        }

        // Check Windows Credential Manager
        var credentialName = $"{_credentialTarget}:{keyIdentifier}";
        return CredentialExists(credentialName);
    }

    public async Task<MachineIdentity> GetMachineIdentityAsync()
    {
        try
        {
            var identity = new MachineIdentity
            {
                MachineName = Environment.MachineName,
                Username = Environment.UserName
            };

            // Get MAC address
            var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(nic => nic.OperationalStatus == OperationalStatus.Up && 
                                      nic.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            
            if (networkInterface != null)
            {
                identity.MacAddress = networkInterface.GetPhysicalAddress().ToString();
            }

            // Get Windows SID
            var windowsIdentity = WindowsIdentity.GetCurrent();
            identity.SecurityIdentifier = windowsIdentity.User?.ToString() ?? "";

            // Get hardware identifiers using WMI
            try
            {
                using var cpuQuery = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                using var cpuResults = cpuQuery.Get();
                var cpuId = cpuResults.Cast<ManagementObject>().FirstOrDefault()?["ProcessorId"]?.ToString();
                identity.CpuId = cpuId ?? "";

                using var motherboardQuery = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                using var motherboardResults = motherboardQuery.Get();
                var motherboardSerial = motherboardResults.Cast<ManagementObject>().FirstOrDefault()?["SerialNumber"]?.ToString();
                identity.MotherboardSerial = motherboardSerial ?? "";
            }
            catch (Exception ex)
            {
                _loggingService.LogWarning($"Could not retrieve hardware identifiers: {ex.Message}", "CryptoService");
            }

            // Create combined identity hash
            var identityString = $"{identity.MacAddress}:{identity.Username}:{identity.MachineName}:{identity.SecurityIdentifier}:{identity.CpuId}:{identity.MotherboardSerial}";
            identity.IdentityHash = ComputeHash(identityString, "SHA256");

            _loggingService.LogDebug("Machine identity retrieved successfully", "CryptoService");
            return identity;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, "Failed to get machine identity", "CryptoService");
            throw;
        }
    }

    public async Task<bool> EncryptFileAsync(string filePath, string? keyIdentifier = null)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        try
        {
            var data = await File.ReadAllBytesAsync(filePath);
            var encryptedData = await EncryptAsync(data, keyIdentifier);
            await File.WriteAllBytesAsync(filePath, encryptedData);
            
            _loggingService.LogInfo($"File encrypted successfully: {filePath}", "CryptoService");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, $"Failed to encrypt file: {filePath}", "CryptoService");
            return false;
        }
    }

    public async Task<bool> DecryptFileAsync(string filePath, string? keyIdentifier = null)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        try
        {
            var encryptedData = await File.ReadAllBytesAsync(filePath);
            var decryptedData = await DecryptAsync(encryptedData, keyIdentifier);
            await File.WriteAllBytesAsync(filePath, decryptedData);
            
            _loggingService.LogInfo($"File decrypted successfully: {filePath}", "CryptoService");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, $"Failed to decrypt file: {filePath}", "CryptoService");
            return false;
        }
    }

    public string GenerateSecureRandomString(int length, bool includeSpecialChars = true)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than 0", nameof(length));

        const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numbers = "0123456789";
        const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        
        var chars = letters + numbers;
        if (includeSpecialChars)
        {
            chars += specialChars;
        }

        var randomBytes = GenerateSecureRandomBytes(length);
        var result = new StringBuilder(length);
        
        for (int i = 0; i < length; i++)
        {
            result.Append(chars[randomBytes[i] % chars.Length]);
        }

        return result.ToString();
    }

    public byte[] GenerateSecureRandomBytes(int length)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than 0", nameof(length));

        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
    }

    private async Task<byte[]> GetOrCreateKeyAsync(string keyIdentifier)
    {
        var key = await RetrieveKeySecurelyAsync(keyIdentifier);
        if (key != null)
        {
            return key;
        }

        // Generate new key if not found
        await GenerateKeyAsync(keyIdentifier);
        key = await RetrieveKeySecurelyAsync(keyIdentifier);
        
        if (key == null)
        {
            throw new InvalidOperationException($"Failed to create or retrieve key: {keyIdentifier}");
        }

        return key;
    }

    private static HashAlgorithm CreateHashAlgorithm(string algorithm)
    {
        return algorithm.ToUpperInvariant() switch
        {
            "SHA256" => SHA256.Create(),
            "SHA512" => SHA512.Create(),
            "SHA1" => SHA1.Create(),
            "MD5" => MD5.Create(),
            _ => throw new ArgumentException($"Unsupported hash algorithm: {algorithm}", nameof(algorithm))
        };
    }

    private static byte[] DeriveKeyFromPassword(string password, string salt, int keyLength)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 10000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(keyLength);
    }

    // Windows Credential Manager integration methods
    private static bool StoreCredential(string target, string password)
    {
        try
        {
            // This is a simplified implementation
            // In a real implementation, you would use Windows Credential Manager APIs
            // For now, we'll use a basic approach with DPAPI
            var protectedData = ProtectedData.Protect(Encoding.UTF8.GetBytes(password), null, DataProtectionScope.CurrentUser);
            var credentialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EnterpriseITAgent", "Credentials");
            Directory.CreateDirectory(credentialPath);
            
            var credentialFile = Path.Combine(credentialPath, Convert.ToBase64String(Encoding.UTF8.GetBytes(target)).Replace('/', '_').Replace('+', '-'));
            File.WriteAllBytes(credentialFile, protectedData);
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? RetrieveCredential(string target)
    {
        try
        {
            var credentialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EnterpriseITAgent", "Credentials");
            var credentialFile = Path.Combine(credentialPath, Convert.ToBase64String(Encoding.UTF8.GetBytes(target)).Replace('/', '_').Replace('+', '-'));
            
            if (!File.Exists(credentialFile))
                return null;
                
            var protectedData = File.ReadAllBytes(credentialFile);
            var unprotectedData = ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(unprotectedData);
        }
        catch
        {
            return null;
        }
    }

    private static bool DeleteCredential(string target)
    {
        try
        {
            var credentialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EnterpriseITAgent", "Credentials");
            var credentialFile = Path.Combine(credentialPath, Convert.ToBase64String(Encoding.UTF8.GetBytes(target)).Replace('/', '_').Replace('+', '-'));
            
            if (File.Exists(credentialFile))
            {
                File.Delete(credentialFile);
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool CredentialExists(string target)
    {
        try
        {
            var credentialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EnterpriseITAgent", "Credentials");
            var credentialFile = Path.Combine(credentialPath, Convert.ToBase64String(Encoding.UTF8.GetBytes(target)).Replace('/', '_').Replace('+', '-'));
            return File.Exists(credentialFile);
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Clear key cache
            lock (_keyCacheLock)
            {
                foreach (var key in _keyCache.Values)
                {
                    Array.Clear(key, 0, key.Length);
                }
                _keyCache.Clear();
            }
            
            _disposed = true;
        }
    }
}