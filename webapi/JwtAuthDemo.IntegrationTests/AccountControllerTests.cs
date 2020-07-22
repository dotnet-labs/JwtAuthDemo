using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JwtAuthDemo.Controllers;
using JwtAuthDemo.Infrastructure;
using JwtAuthDemo.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JwtAuthDemo.IntegrationTests
{
    [TestClass]
    public class AccountControllerTests
    {
        private readonly TestHostFixture _testHostFixture = new TestHostFixture();
        private HttpClient _httpClient;
        private IServiceProvider _serviceProvider;

        [TestInitialize]
        public void SetUp()
        {
            _httpClient = _testHostFixture.Client;
            _serviceProvider = _testHostFixture.ServiceProvider;
        }

        [TestMethod]
        public async Task ShouldExpect401WhenLoginWithInvalidCredentials()
        {
            var credentials = new LoginRequest
            {
                UserName = "admin",
                Password = "invalidPassword"
            };
            var response = await _httpClient.PostAsync("api/account/login",
                new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, MediaTypeNames.Application.Json));
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task ShouldReturnCorrectResponseForSuccessLogin()
        {
            var credentials = new LoginRequest
            {
                UserName = "admin",
                Password = "securePassword"
            };
            var loginResponse = await _httpClient.PostAsync("api/account/login",
                new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, MediaTypeNames.Application.Json));
            Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);

            var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<LoginResult>(loginResponseContent);
            Assert.AreEqual(credentials.UserName, loginResult.UserName);
            Assert.IsNull(loginResult.OriginalUserName);
            Assert.AreEqual(UserRoles.Admin, loginResult.Role);
            Assert.IsFalse(string.IsNullOrWhiteSpace(loginResult.AccessToken));
            Assert.IsFalse(string.IsNullOrWhiteSpace(loginResult.RefreshToken));

            var jwtAuthManager = _serviceProvider.GetRequiredService<IJwtAuthManager>();
            var (principal, jwtSecurityToken) = jwtAuthManager.DecodeJwtToken(loginResult.AccessToken);
            Assert.AreEqual(credentials.UserName, principal.Identity.Name);
            Assert.AreEqual(UserRoles.Admin, principal.FindFirst(ClaimTypes.Role).Value);
            Assert.IsNotNull(jwtSecurityToken);
        }

        [TestMethod]
        public async Task ShouldBeAbleToLogout()
        {
            var credentials = new LoginRequest
            {
                UserName = "admin",
                Password = "securePassword"
            };
            var loginResponse = await _httpClient.PostAsync("api/account/login",
                new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, MediaTypeNames.Application.Json));
            var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<LoginResult>(loginResponseContent);

            var jwtAuthManager = _serviceProvider.GetRequiredService<IJwtAuthManager>();
            Assert.IsTrue(jwtAuthManager.UsersRefreshTokensReadOnlyDictionary.ContainsKey(loginResult.RefreshToken));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, loginResult.AccessToken);
            var logoutResponse = await _httpClient.PostAsync("api/account/logout", null);
            Assert.AreEqual(HttpStatusCode.OK, logoutResponse.StatusCode);
            Assert.IsFalse(jwtAuthManager.UsersRefreshTokensReadOnlyDictionary.ContainsKey(loginResult.RefreshToken));
        }

        [TestMethod]
        public async Task ShouldCorrectlyRefreshToken()
        {
            const string userName = "admin";
            var claims = new[]
            {
                new Claim(ClaimTypes.Name,userName),
                new Claim(ClaimTypes.Role, UserRoles.Admin)
            };
            var jwtAuthManager = _serviceProvider.GetRequiredService<IJwtAuthManager>();
            var jwtResult = jwtAuthManager.GenerateTokens(userName, claims, DateTime.Now.AddMinutes(-1));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwtResult.AccessToken);
            var refreshRequest = new RefreshTokenRequest
            {
                RefreshToken = jwtResult.RefreshToken.TokenString
            };
            var response = await _httpClient.PostAsync("api/account/refresh-token",
                new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, MediaTypeNames.Application.Json));
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResult>(responseContent);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var refreshToken2 = jwtAuthManager.UsersRefreshTokensReadOnlyDictionary.GetValueOrDefault(result.RefreshToken);
            Assert.AreEqual(refreshToken2.TokenString, result.RefreshToken);
            Assert.AreNotEqual(refreshToken2.TokenString, jwtResult.RefreshToken.TokenString);
            Assert.AreNotEqual(jwtResult.AccessToken, result.AccessToken);
        }


        [TestMethod]
        public async Task ShouldNotAllowToRefreshTokenWhenRefreshTokenIsExpired()
        {
            const string userName = "admin";
            var claims = new[]
            {
                new Claim(ClaimTypes.Name,userName),
                new Claim(ClaimTypes.Role, UserRoles.Admin)
            };
            var jwtAuthManager = _serviceProvider.GetRequiredService<IJwtAuthManager>();
            var jwtTokenConfig = _serviceProvider.GetRequiredService<JwtTokenConfig>();
            var jwtResult1 = jwtAuthManager.GenerateTokens(userName, claims, DateTime.Now.AddMinutes(-jwtTokenConfig.RefreshTokenExpiration - 1));
            var jwtResult2 = jwtAuthManager.GenerateTokens(userName, claims, DateTime.Now.AddMinutes(-1));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwtResult2.AccessToken); // valid JWT token
            var refreshRequest = new RefreshTokenRequest
            {
                RefreshToken = jwtResult1.RefreshToken.TokenString
            };
            var response = await _httpClient.PostAsync("api/account/refresh-token",
                new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, MediaTypeNames.Application.Json)); // expired Refresh token
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.AreEqual("Invalid token", responseContent);
        }

        [TestMethod]
        public async Task ShouldAllowAdminImpersonateOthers()
        {
            const string userName = "admin";
            var claims = new[]
            {
                new Claim(ClaimTypes.Name,userName),
                new Claim(ClaimTypes.Role, UserRoles.Admin)
            };
            var jwtAuthManager = _serviceProvider.GetRequiredService<IJwtAuthManager>();
            var jwtResult = jwtAuthManager.GenerateTokens(userName, claims, DateTime.Now.AddMinutes(-1));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwtResult.AccessToken);
            var request = new ImpersonationRequest { UserName = "test1" };
            var response = await _httpClient.PostAsync("api/account/impersonation",
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, MediaTypeNames.Application.Json));
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResult>(responseContent);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(request.UserName, result.UserName);
            Assert.AreEqual(userName, result.OriginalUserName);
            Assert.AreEqual(UserRoles.BasicUser, result.Role);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.AccessToken));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.RefreshToken));

            var (principal, jwtSecurityToken) = jwtAuthManager.DecodeJwtToken(result.AccessToken);
            Assert.AreEqual(request.UserName, principal.Identity.Name);
            Assert.AreEqual(UserRoles.BasicUser, principal.FindFirst(ClaimTypes.Role).Value);
            Assert.AreEqual(userName, principal.FindFirst("OriginalUserName").Value);
            Assert.IsNotNull(jwtSecurityToken);
        }

        [TestMethod]
        public async Task ShouldForbidNonAdminToImpersonate()
        {
            const string userName = "test1";
            var claims = new[]
            {
                new Claim(ClaimTypes.Name,userName),
                new Claim(ClaimTypes.Role, UserRoles.BasicUser)
            };
            var jwtAuthManager = _serviceProvider.GetRequiredService<IJwtAuthManager>();
            var jwtResult = jwtAuthManager.GenerateTokens(userName, claims, DateTime.Now.AddMinutes(-1));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwtResult.AccessToken);
            var request = new ImpersonationRequest { UserName = "test2" };
            var response = await _httpClient.PostAsync("api/account/impersonation",
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, MediaTypeNames.Application.Json));

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task ShouldAllowAdminToStopImpersonation()
        {
            const string userName = "test1";
            const string originalUserName = "admin";
            var claims = new[]
            {
                new Claim(ClaimTypes.Name,userName),
                new Claim(ClaimTypes.Role, UserRoles.BasicUser),
                new Claim("OriginalUserName", originalUserName)
            };
            var jwtAuthManager = _serviceProvider.GetRequiredService<IJwtAuthManager>();
            var jwtResult = jwtAuthManager.GenerateTokens(userName, claims, DateTime.Now.AddMinutes(-1));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwtResult.AccessToken);
            var response = await _httpClient.PostAsync("api/account/stop-impersonation", null);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResult>(responseContent);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(originalUserName, result.UserName);
            Assert.IsTrue(string.IsNullOrWhiteSpace(result.OriginalUserName));
            Assert.AreEqual(UserRoles.Admin, result.Role);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.AccessToken));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.RefreshToken));

            var (principal, jwtSecurityToken) = jwtAuthManager.DecodeJwtToken(result.AccessToken);
            Assert.AreEqual(originalUserName, principal.Identity.Name);
            Assert.AreEqual(UserRoles.Admin, principal.FindFirst(ClaimTypes.Role).Value);
            Assert.IsTrue(string.IsNullOrWhiteSpace(principal.FindFirst("OriginalUserName")?.Value));
            Assert.IsNotNull(jwtSecurityToken);
        }

        [TestMethod]
        public async Task ShouldReturnBadRequestIfStopImpersonationWhenNotImpersonating()
        {
            const string userName = "test1";
            var claims = new[]
            {
                new Claim(ClaimTypes.Name,userName),
                new Claim(ClaimTypes.Role, UserRoles.BasicUser)
            };
            var jwtAuthManager = _serviceProvider.GetRequiredService<IJwtAuthManager>();
            var jwtResult = jwtAuthManager.GenerateTokens(userName, claims, DateTime.Now.AddMinutes(-1));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwtResult.AccessToken);
            var request = new ImpersonationRequest { UserName = "test2" };
            var response = await _httpClient.PostAsync("api/account/stop-impersonation",
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, MediaTypeNames.Application.Json));

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
