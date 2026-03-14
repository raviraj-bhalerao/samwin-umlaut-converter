using System;
using Samwin.UmlautConverter.Api.Services;
using Xunit;

namespace Samwin.UmlautConverter.Api.Tests
{
    public class EnvironmentAuthConfigProviderTests : IDisposable
    {
        private const string TokenKeyEnvVar = "TokenKey";
        private const string TokenIssuerEnvVar = "TokenIssuer";
        private const string TokenAudienceEnvVar = "TokenAudience";
        private readonly EnvironmentAuthConfigProvider _provider;

        public EnvironmentAuthConfigProviderTests()
        {
            _provider = new EnvironmentAuthConfigProvider();
            // Clean up before each test to ensure isolation
            Dispose();
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(TokenKeyEnvVar, null);
            Environment.SetEnvironmentVariable(TokenIssuerEnvVar, null);
            Environment.SetEnvironmentVariable(TokenAudienceEnvVar, null);
        }

        [Fact]
        public void GetSecretKey_WhenKeyIsValid_ReturnsKey()
        {
            // Arrange
            var expectedKey = "a_valid_key_that_is_long_enough_for_sha256";
            Environment.SetEnvironmentVariable(TokenKeyEnvVar, expectedKey);

            // Act
            var actualKey = _provider.GetSecretKey();

            // Assert
            Assert.Equal(expectedKey, actualKey);
        }

        [Fact]
        public void GetSecretKey_WhenKeyIsNotSet_ThrowsInvalidOperationException()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _provider.GetSecretKey());
            Assert.Contains($"'{TokenKeyEnvVar}' is not set", exception.Message);
        }

        [Fact]
        public void GetSecretKey_WhenKeyIsTooShort_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable(TokenKeyEnvVar, "short-key");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _provider.GetSecretKey());
            Assert.Contains($"'{TokenKeyEnvVar}' must be at least 32 characters", exception.Message);
        }

        [Fact]
        public void GetIssuer_WhenIssuerIsSet_ReturnsIssuer()
        {
            // Arrange
            var expectedIssuer = "test-issuer";
            Environment.SetEnvironmentVariable(TokenIssuerEnvVar, expectedIssuer);

            // Act
            var actualIssuer = _provider.GetIssuer();

            // Assert
            Assert.Equal(expectedIssuer, actualIssuer);
        }

        [Fact]
        public void GetIssuer_WhenIssuerIsNotSet_ThrowsInvalidOperationException()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _provider.GetIssuer());
            Assert.Contains($"'{TokenIssuerEnvVar}' is not set", exception.Message);
        }

        [Fact]
        public void GetAudience_WhenAudienceIsSet_ReturnsAudience()
        {
            // Arrange
            var expectedAudience = "test-audience";
            Environment.SetEnvironmentVariable(TokenAudienceEnvVar, expectedAudience);

            // Act
            var actualAudience = _provider.GetAudience();

            // Assert
            Assert.Equal(expectedAudience, actualAudience);
        }

        [Fact]
        public void GetAudience_WhenAudienceIsNotSet_ThrowsInvalidOperationException()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _provider.GetAudience());
            Assert.Contains($"'{TokenAudienceEnvVar}' is not set", exception.Message);
        }
    }
}