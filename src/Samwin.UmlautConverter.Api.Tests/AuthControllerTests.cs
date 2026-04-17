using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Samwin.UmlautConverter.Api.Controllers;
using Samwin.UmlautConverter.Api.Services;
using Samwin.UmlautConverter.Api.Services.Jwt;
using Samwin.UmlautConverter.Api.Services.Telemetry;
using Xunit;

namespace Samwin.UmlautConverter.Api.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<ICreateTokenService> _mockTokenService;
        private readonly Mock<MetricsService> _mockMetricService;
        private readonly Mock<IActivityService> _mockActivityService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockTokenService = new Mock<ICreateTokenService>();
            _mockMetricService = new Mock<MetricsService>();
            _mockActivityService = new Mock<IActivityService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(_mockTokenService.Object, _mockActivityService.Object, 
            _mockMetricService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task LoginWithPassword_WithValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var request = new LoginRequest { UserName = "bhaleraor", Password = "bhaleraor@1234" };
            var expectedToken = "fake-jwt-token-for-testing";

            _mockTokenService
                .Setup(s => s.CreateToken(request.UserName, request.UserName, It.IsAny<string[]>()))
                .Returns(expectedToken);

            // Act
            var result = await _controller.LoginWithPassword(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic? resultValue = okResult.Value;
            Assert.NotNull(resultValue);
            Assert.Equal(expectedToken, resultValue!.GetType().GetProperty("token")?.GetValue(resultValue, null));

            // Verify that the token service was called exactly once with the correct user's roles
            _mockTokenService.Verify(
                s => s.CreateToken(request.UserName, request.UserName, It.Is<string[]>(roles => roles.Contains("TechCaptain"))),
                Times.Once);
        }

        [Theory]
        [InlineData("bhaleraor", "wrong-password")] // Invalid password
        [InlineData("unknown-user", "any-password")] // Non-existent user
        public async Task LoginWithPassword_WithInvalidCredentials_ReturnsUnauthorized(string userName, string password)
        {
            // Arrange
            var request = new LoginRequest { UserName = userName, Password = password };

            // Act
            var result = await _controller.LoginWithPassword(request);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);

            // Verify that the token service was NEVER called
            _mockTokenService.Verify(
                s => s.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()),
                Times.Never);
        }

        [Fact]
        public async Task LoginWithPassword_WhenTokenServiceThrows_PropagatesException()
        {
            // Arrange
            var request = new LoginRequest { UserName = "bhaleraor", Password = "bhaleraor@1234" };

            _mockTokenService
                .Setup(s => s.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
                .Throws(new System.InvalidOperationException("Critical failure"));

            // Act & Assert
            await Assert.ThrowsAsync<System.InvalidOperationException>(() => _controller.LoginWithPassword(request));

            // Verify that the service WAS called exactly once (which is what caused the exception)
            _mockTokenService.Verify(
                s => s.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()),
                Times.Once);
        }
    }
}