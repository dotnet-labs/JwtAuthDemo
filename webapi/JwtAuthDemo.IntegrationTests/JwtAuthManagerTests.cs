using System;
using System.Security.Claims;
using JwtAuthDemo.Infrastructure;
using JwtAuthDemo.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JwtAuthDemo.IntegrationTests
{
    [TestClass]
    public class JwtAuthManagerTests
    {
        private readonly TestHostFixture _testHostFixture = new TestHostFixture();
        private IServiceProvider _serviceProvider;

        [TestInitialize]
        public void SetUp()
        {
            _serviceProvider = _testHostFixture.ServiceProvider;
        }

        [TestMethod]
        public void ShouldLoadCorrectJwtConfig()
        {
            var jwtConfig = _serviceProvider.GetRequiredService<JwtTokenConfig>();
            Assert.AreEqual("1234567890123456789", jwtConfig.Secret);
            Assert.AreEqual(20, jwtConfig.AccessTokenExpiration);
            Assert.AreEqual(60, jwtConfig.RefreshTokenExpiration);
        }

        [TestMethod]
        public void ShouldRotateRefreshToken()
        {
            var jwtConfig = _serviceProvider.GetRequiredService<JwtTokenConfig>();
            var jwtAuthManager = _serviceProvider.GetRequiredService<IJwtAuthManager>();
            var now = DateTime.Now;
            const string userName = "admin";
            var claims = new[]
            {
                new Claim(ClaimTypes.Name,userName),
                new Claim(ClaimTypes.Role, UserRoles.Admin)
            };

            var tokens1 = jwtAuthManager.GenerateTokens(userName, claims, now.AddMinutes(-20));
            var tokens2 = jwtAuthManager.Refresh(tokens1.RefreshToken.TokenString, tokens1.AccessToken, now);

            Assert.AreNotEqual(tokens1.AccessToken, tokens2.AccessToken);
            Assert.AreNotEqual(tokens1.RefreshToken.TokenString, tokens2.RefreshToken.TokenString);
            Assert.AreEqual(now.AddMinutes(jwtConfig.RefreshTokenExpiration - 20), tokens1.RefreshToken.ExpireAt);
            Assert.AreEqual(now.AddMinutes(jwtConfig.RefreshTokenExpiration), tokens2.RefreshToken.ExpireAt);
            Assert.AreEqual(userName, tokens2.RefreshToken.UserName);
        }

        [TestMethod]
        public void ShouldThrowExceptionWhenRefreshTokenUsingAnExpiredToken()
        {
            var jwtAuthManager = _serviceProvider.GetRequiredService<IJwtAuthManager>();
            var jwtTokenConfig = _serviceProvider.GetRequiredService<JwtTokenConfig>();
            const string userName = "admin";
            var now = DateTime.Now;
            var claims = new[]
            {
                new Claim(ClaimTypes.Name,userName),
                new Claim(ClaimTypes.Role, UserRoles.Admin)
            };
            var tokens = jwtAuthManager.GenerateTokens(userName, claims, now.AddMinutes(-jwtTokenConfig.RefreshTokenExpiration - 1));

            var e = Assert.ThrowsException<SecurityTokenException>(() => jwtAuthManager.Refresh(tokens.RefreshToken.TokenString, tokens.AccessToken, now));
            Assert.AreEqual("Invalid token", e.Message);
        }
    }
}
