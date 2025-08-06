using System;
using System.Threading.Tasks;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// Interface for cryptographic services including AES-256 encryption and key management
/// </summary>
public interface ICryptoService
{
    /// <summary>
    /// Encrypts data using AES-256 encryption
    /// </summary>
    /// <param name="plainText">Data to encrypt</param>
    /// <param name="keyIdentifier">Optional key identifier, uses node key if null</param>
    /// <returns>Encrypted data as base64 string</returns>
    Task<string> EncryptAsync(string plainText, string? keyIdentifier = null);

    /// <summary>
    /// Encrypts binary data using AES-256 encryption
    /// </summary>
    /// <param name="data">Binary data to encrypt</param>
    /// <param name="keyIdentifier">Optional key identifier, uses node key if null</param>
    /// <returns>Encrypted data</returns>
    Task<byte[]> EncryptAsync(byte[] data, string? keyIdentifier = null);

    /// <summary>
    /// Decrypts data using AES-256 encryption
    /// </summary>
    /// <param name="encryptedData">Encrypted data as base64 string</param>
    /// <param name="keyIdentifier">Optional key identifier, uses node key if null</param>
    /// <returns>Decrypted plain text</returns>
    Task<string> DecryptAsync(string encryptedData, string? keyIdentifier = null);

    /// <summary>
    /// Decrypts binary data using AES-256 encryption
    /// </summary>
    /// <param name="encryptedData">Encrypted binary data</param>
    /// <param name="keyIdentifier">Optional key identifier, uses node key if null</param>
    /// <returns>Decrypted binary data</returns>
    Task<byte[]> DecryptAsync(byte[] encryptedData, string? keyIdentifier = null);

    /// <summary>
    /// Generates a cryptographic hash of the input data
    /// </summary>
    /// <param name="data">Data to hash</param>
    /// <param name="algorithm">Hash algorithm (SHA256, SHA512)</param>
    /// <returns>Hash as hexadecimal string</returns>
    string ComputeHash(byte[] data, string algorithm = "SHA256");

    /// <summary>
    /// Generates a cryptographic hash of the input string
    /// </summary>
    /// <param name="input">String to hash</param>
    /// <param name="algorithm">Hash algorithm (SHA256, SHA512)</param>
    /// <returns>Hash as hexadecimal string</returns>
    string ComputeHash(string input, string algorithm = "SHA256");

    /// <summary>
    /// Verifies a hash against input data
    /// </summary>
    /// <param name="data">Original data</param>
    /// <param name="hash">Hash to verify</param>
    /// <param name="algorithm">Hash algorithm used</param>
    /// <returns>True if hash matches</returns>
    bool VerifyHash(byte[] data, string hash, string algorithm = "SHA256");

    /// <summary>
    /// Verifies a hash against input string
    /// </summary>
    /// <param name="input">Original string</param>
    /// <param name="hash">Hash to verify</param>
    /// <param name="algorithm">Hash algorithm used</param>
    /// <returns>True if hash matches</returns>
    bool VerifyHash(string input, string hash, string algorithm = "SHA256");

    /// <summary>
    /// Derives a node-specific encryption key from machine identity
    /// </summary>
    /// <param name="nodeId">Node identifier</param>
    /// <param name="additionalEntropy">Additional entropy for key derivation</param>
    /// <returns>Derived key identifier</returns>
    Task<string> DeriveNodeKeyAsync(string nodeId, string? additionalEntropy = null);

    /// <summary>
    /// Generates a new encryption key
    /// </summary>
    /// <param name="keyIdentifier">Identifier for the new key</param>
    /// <param name="keySize">Key size in bits (default 256)</param>
    /// <returns>Key identifier</returns>
    Task<string> GenerateKeyAsync(string keyIdentifier, int keySize = 256);

    /// <summary>
    /// Securely stores a key in Windows Credential Manager
    /// </summary>
    /// <param name="keyIdentifier">Key identifier</param>
    /// <param name="keyData">Key data to store</param>
    /// <returns>True if successful</returns>
    Task<bool> StoreKeySecurelyAsync(string keyIdentifier, byte[] keyData);

    /// <summary>
    /// Retrieves a key from Windows Credential Manager
    /// </summary>
    /// <param name="keyIdentifier">Key identifier</param>
    /// <returns>Key data or null if not found</returns>
    Task<byte[]?> RetrieveKeySecurelyAsync(string keyIdentifier);

    /// <summary>
    /// Deletes a key from Windows Credential Manager
    /// </summary>
    /// <param name="keyIdentifier">Key identifier</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteKeySecurelyAsync(string keyIdentifier);

    /// <summary>
    /// Checks if a key exists in secure storage
    /// </summary>
    /// <param name="keyIdentifier">Key identifier</param>
    /// <returns>True if key exists</returns>
    Task<bool> KeyExistsAsync(string keyIdentifier);

    /// <summary>
    /// Gets machine identity information for key derivation
    /// </summary>
    /// <returns>Machine identity data</returns>
    Task<MachineIdentity> GetMachineIdentityAsync();

    /// <summary>
    /// Encrypts a file in place
    /// </summary>
    /// <param name="filePath">Path to file to encrypt</param>
    /// <param name="keyIdentifier">Optional key identifier</param>
    /// <returns>True if successful</returns>
    Task<bool> EncryptFileAsync(string filePath, string? keyIdentifier = null);

    /// <summary>
    /// Decrypts a file in place
    /// </summary>
    /// <param name="filePath">Path to file to decrypt</param>
    /// <param name="keyIdentifier">Optional key identifier</param>
    /// <returns>True if successful</returns>
    Task<bool> DecryptFileAsync(string filePath, string? keyIdentifier = null);

    /// <summary>
    /// Generates a secure random string
    /// </summary>
    /// <param name="length">Length of random string</param>
    /// <param name="includeSpecialChars">Include special characters</param>
    /// <returns>Secure random string</returns>
    string GenerateSecureRandomString(int length, bool includeSpecialChars = true);

    /// <summary>
    /// Generates secure random bytes
    /// </summary>
    /// <param name="length">Number of bytes to generate</param>
    /// <returns>Secure random bytes</returns>
    byte[] GenerateSecureRandomBytes(int length);
}

/// <summary>
/// Machine identity information for key derivation
/// </summary>
public class MachineIdentity
{
    /// <summary>
    /// Primary MAC address of the machine
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>
    /// Current Windows username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Machine name
    /// </summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Windows SID (Security Identifier)
    /// </summary>
    public string SecurityIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// CPU identifier
    /// </summary>
    public string CpuId { get; set; } = string.Empty;

    /// <summary>
    /// Motherboard serial number
    /// </summary>
    public string MotherboardSerial { get; set; } = string.Empty;

    /// <summary>
    /// Combined identity hash for key derivation
    /// </summary>
    public string IdentityHash { get; set; } = string.Empty;
}