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

            var jwtAuthResult1 = jwtAuthManager.GenerateTokens(userName, claims, now.AddMinutes(-jwtTokenConfig.AccessTokenExpiration - 1).AddSeconds(1));
            jwtAuthManager.Refresh(jwtAuthResult1.RefreshToken.TokenString, jwtAuthResult1.AccessToken, now);

            var jwtAuthResult2 = jwtAuthManager.GenerateTokens(userName, claims, now.AddMinutes(-jwtTokenConfig.AccessTokenExpiration - 1));
            Assert.ThrowsException<SecurityTokenExpiredException>(() => jwtAuthManager.Refresh(jwtAuthResult2.RefreshToken.TokenString, jwtAuthResult2.AccessToken, now));
        }

        [TestMethod]
        public void ShouldThrowExceptionWhenRefreshTokenIsForged()
        {
            var jwtAuthManager = _serviceProvider.GetRequiredService<IJwtAuthManager>();
            var jwtTokenConfig = _serviceProvider.GetRequiredService<JwtTokenConfig>();
            var now = DateTime.Now;

            var claims1 = new[]
            {
                new Claim(ClaimTypes.Name,"admin"),
                new Claim(ClaimTypes.Role, UserRoles.Admin)
            };
            var tokens1 = jwtAuthManager.GenerateTokens("admin", claims1, now.AddMinutes(-jwtTokenConfig.AccessTokenExpiration));

            var claims2 = new[]
            {
                new Claim(ClaimTypes.Name,"test1"),
                new Claim(ClaimTypes.Role, UserRoles.Admin)
            };
            var tokens2 = jwtAuthManager.GenerateTokens("test1", claims2, now.AddMinutes(-jwtTokenConfig.AccessTokenExpiration));

            // forge a token: try to use the refresh token for "test1", but use the access token for "admin"
            var e = Assert.ThrowsException<SecurityTokenException>(() => jwtAuthManager.Refresh(tokens2.RefreshToken.TokenString, tokens1.AccessToken, now));
            Assert.AreEqual("Invalid token", e.Message);
        }
    }
}
