using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EnterpriseITAgent.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EnterpriseITAgent.Tests.Infrastructure;

public class CryptoServiceTests : IDisposable
{
    private readonly Mock<ILogger<CryptoService>> _mockLogger;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly CryptoService _cryptoService;

    public CryptoServiceTests()
    {
        _mockLogger = new Mock<ILogger<CryptoService>>();
        _mockLoggingService = new Mock<ILoggingService>();
        _cryptoService = new CryptoService(_mockLogger.Object, _mockLoggingService.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitialize()
    {
        // Arrange & Act
        var cryptoService = new CryptoService(_mockLogger.Object, _mockLoggingService.Object);

        // Assert
        Assert.NotNull(cryptoService);
        _mockLoggingService.Verify(x => x.LogInfo(
            "CryptoService initialized with AES-256 encryption",
            "CryptoService",
            It.IsAny<System.Collections.Generic.Dictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CryptoService(null!, _mockLoggingService.Object));
    }

    [Fact]
    public void Constructor_WithNullLoggingService_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CryptoService(_mockLogger.Object, null!));
    }

    [Fact]
    public async Task EncryptAsync_WithValidString_ShouldReturnEncryptedData()
    {
        // Arrange
        var plainText = "Hello, World!";

        // Act
        var encryptedData = await _cryptoService.EncryptAsync(plainText);

        // Assert
        Assert.NotNull(encryptedData);
        Assert.NotEmpty(encryptedData);
        Assert.NotEqual(plainText, encryptedData);
    }

    [Fact]
    public async Task EncryptAsync_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _cryptoService.EncryptAsync(string.Empty));
    }

    [Fact]
    public async Task EncryptAsync_WithNullString_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _cryptoService.EncryptAsync((string)null!));
    }

    [Fact]
    public async Task EncryptAsync_WithValidBytes_ShouldReturnEncryptedData()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Hello, World!");

        // Act
        var encryptedData = await _cryptoService.EncryptAsync(data);

        // Assert
        Assert.NotNull(encryptedData);
        Assert.True(encryptedData.Length > 0);
        Assert.False(data.AsSpan().SequenceEqual(encryptedData));
    }

    [Fact]
    public async Task EncryptAsync_WithEmptyBytes_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _cryptoService.EncryptAsync(Array.Empty<byte>()));
    }

    [Fact]
    public async Task EncryptAsync_WithNullBytes_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _cryptoService.EncryptAsync((byte[])null!));
    }

    [Fact]
    public async Task DecryptAsync_WithValidEncryptedString_ShouldReturnOriginalText()
    {
        // Arrange
        var plainText = "Hello, World!";
        var encryptedData = await _cryptoService.EncryptAsync(plainText);

        // Act
        var decryptedText = await _cryptoService.DecryptAsync(encryptedData);

        // Assert
        Assert.Equal(plainText, decryptedText);
    }

    [Fact]
    public async Task DecryptAsync_WithValidEncryptedBytes_ShouldReturnOriginalData()
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("Hello, World!");
        var encryptedData = await _cryptoService.EncryptAsync(originalData);

        // Act
        var decryptedData = await _cryptoService.DecryptAsync(encryptedData);

        // Assert
        Assert.True(originalData.AsSpan().SequenceEqual(decryptedData));
    }

    [Fact]
    public async Task EncryptDecrypt_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var testData = "This is a test message with special characters: !@#$%^&*()";

        // Act
        var encrypted = await _cryptoService.EncryptAsync(testData);
        var decrypted = await _cryptoService.DecryptAsync(encrypted);

        // Assert
        Assert.Equal(testData, decrypted);
    }

    [Fact]
    public void ComputeHash_WithValidString_ShouldReturnHash()
    {
        // Arrange
        var input = "Hello, World!";

        // Act
        var hash = _cryptoService.ComputeHash(input);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.Equal(64, hash.Length); // SHA256 produces 64 character hex string
    }

    [Fact]
    public void ComputeHash_WithValidBytes_ShouldReturnHash()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Hello, World!");

        // Act
        var hash = _cryptoService.ComputeHash(data);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.Equal(64, hash.Length); // SHA256 produces 64 character hex string
    }

    [Fact]
    public void ComputeHash_WithSameInput_ShouldReturnSameHash()
    {
        // Arrange
        var input = "Test input";

        // Act
        var hash1 = _cryptoService.ComputeHash(input);
        var hash2 = _cryptoService.ComputeHash(input);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_WithDifferentInputs_ShouldReturnDifferentHashes()
    {
        // Arrange
        var input1 = "Test input 1";
        var input2 = "Test input 2";

        // Act
        var hash1 = _cryptoService.ComputeHash(input1);
        var hash2 = _cryptoService.ComputeHash(input2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyHash_WithValidHash_ShouldReturnTrue()
    {
        // Arrange
        var input = "Hello, World!";
        var hash = _cryptoService.ComputeHash(input);

        // Act
        var isValid = _cryptoService.VerifyHash(input, hash);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyHash_WithInvalidHash_ShouldReturnFalse()
    {
        // Arrange
        var input = "Hello, World!";
        var invalidHash = "invalid_hash";

        // Act
        var isValid = _cryptoService.VerifyHash(input, invalidHash);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifyHash_WithModifiedInput_ShouldReturnFalse()
    {
        // Arrange
        var originalInput = "Hello, World!";
        var modifiedInput = "Hello, World!!";
        var hash = _cryptoService.ComputeHash(originalInput);

        // Act
        var isValid = _cryptoService.VerifyHash(modifiedInput, hash);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task DeriveNodeKeyAsync_WithValidNodeId_ShouldReturnKeyIdentifier()
    {
        // Arrange
        var nodeId = "test-node-123";

        // Act
        var keyIdentifier = await _cryptoService.DeriveNodeKeyAsync(nodeId);

        // Assert
        Assert.NotNull(keyIdentifier);
        Assert.Contains(nodeId, keyIdentifier);
        Assert.True(await _cryptoService.KeyExistsAsync(keyIdentifier));
    }

    [Fact]
    public async Task DeriveNodeKeyAsync_WithEmptyNodeId_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _cryptoService.DeriveNodeKeyAsync(string.Empty));
    }

    [Fact]
    public async Task DeriveNodeKeyAsync_WithNullNodeId_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _cryptoService.DeriveNodeKeyAsync(null!));
    }

    [Fact]
    public async Task GenerateKeyAsync_WithValidIdentifier_ShouldCreateKey()
    {
        // Arrange
        var keyIdentifier = "test-key-123";

        // Act
        var result = await _cryptoService.GenerateKeyAsync(keyIdentifier);

        // Assert
        Assert.Equal(keyIdentifier, result);
        Assert.True(await _cryptoService.KeyExistsAsync(keyIdentifier));
    }

    [Fact]
    public async Task GenerateKeyAsync_WithInvalidKeySize_ShouldThrowArgumentException()
    {
        // Arrange
        var keyIdentifier = "test-key";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _cryptoService.GenerateKeyAsync(keyIdentifier, 100));
    }

    [Fact]
    public async Task StoreRetrieveKey_ShouldWorkCorrectly()
    {
        // Arrange
        var keyIdentifier = "test-store-key";
        var keyData = _cryptoService.GenerateSecureRandomBytes(32);

        // Act
        var stored = await _cryptoService.StoreKeySecurelyAsync(keyIdentifier, keyData);
        var retrieved = await _cryptoService.RetrieveKeySecurelyAsync(keyIdentifier);

        // Assert
        Assert.True(stored);
        Assert.NotNull(retrieved);
        Assert.True(keyData.AsSpan().SequenceEqual(retrieved));
    }

    [Fact]
    public async Task DeleteKeySecurelyAsync_WithExistingKey_ShouldDeleteKey()
    {
        // Arrange
        var keyIdentifier = "test-delete-key";
        var keyData = _cryptoService.GenerateSecureRandomBytes(32);
        await _cryptoService.StoreKeySecurelyAsync(keyIdentifier, keyData);

        // Act
        var deleted = await _cryptoService.DeleteKeySecurelyAsync(keyIdentifier);
        var exists = await _cryptoService.KeyExistsAsync(keyIdentifier);

        // Assert
        Assert.True(deleted);
        Assert.False(exists);
    }

    [Fact]
    public async Task GetMachineIdentityAsync_ShouldReturnValidIdentity()
    {
        // Act
        var identity = await _cryptoService.GetMachineIdentityAsync();

        // Assert
        Assert.NotNull(identity);
        Assert.NotEmpty(identity.MachineName);
        Assert.NotEmpty(identity.Username);
        Assert.NotEmpty(identity.IdentityHash);
    }

    [Fact]
    public async Task EncryptDecryptFile_ShouldWorkCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var originalContent = "This is test file content for encryption testing.";
        await File.WriteAllTextAsync(tempFile, originalContent);

        try
        {
            // Act
            var encrypted = await _cryptoService.EncryptFileAsync(tempFile);
            var encryptedContent = await File.ReadAllTextAsync(tempFile);
            
            var decrypted = await _cryptoService.DecryptFileAsync(tempFile);
            var decryptedContent = await File.ReadAllTextAsync(tempFile);

            // Assert
            Assert.True(encrypted);
            Assert.True(decrypted);
            Assert.NotEqual(originalContent, encryptedContent);
            Assert.Equal(originalContent, decryptedContent);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void GenerateSecureRandomString_WithValidLength_ShouldReturnRandomString()
    {
        // Arrange
        var length = 16;

        // Act
        var randomString = _cryptoService.GenerateSecureRandomString(length);

        // Assert
        Assert.NotNull(randomString);
        Assert.Equal(length, randomString.Length);
    }

    [Fact]
    public void GenerateSecureRandomString_WithZeroLength_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => _cryptoService.GenerateSecureRandomString(0));
    }

    [Fact]
    public void GenerateSecureRandomBytes_WithValidLength_ShouldReturnRandomBytes()
    {
        // Arrange
        var length = 32;

        // Act
        var randomBytes = _cryptoService.GenerateSecureRandomBytes(length);

        // Assert
        Assert.NotNull(randomBytes);
        Assert.Equal(length, randomBytes.Length);
    }

    [Fact]
    public void GenerateSecureRandomBytes_WithZeroLength_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => _cryptoService.GenerateSecureRandomBytes(0));
    }

    [Fact]
    public void GenerateSecureRandomBytes_MultipleCalls_ShouldReturnDifferentValues()
    {
        // Arrange
        var length = 16;

        // Act
        var bytes1 = _cryptoService.GenerateSecureRandomBytes(length);
        var bytes2 = _cryptoService.GenerateSecureRandomBytes(length);

        // Assert
        Assert.False(bytes1.AsSpan().SequenceEqual(bytes2));
    }

    public void Dispose()
    {
        _cryptoService?.Dispose();
    }
}