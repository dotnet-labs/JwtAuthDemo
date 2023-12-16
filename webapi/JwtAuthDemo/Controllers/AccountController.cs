using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json.Serialization;
using JwtAuthDemo.Infrastructure;
using JwtAuthDemo.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace JwtAuthDemo.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AccountController(
    ILogger<AccountController> logger,
    IUserService userService,
    IJwtAuthManager jwtAuthManager)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        if (!userService.IsValidUserCredentials(request.UserName, request.Password))
        {
            return Unauthorized();
        }

        var role = userService.GetUserRole(request.UserName);
        var claims = new[]
        {
            new Claim(ClaimTypes.Name,request.UserName),
            new Claim(ClaimTypes.Role, role)
        };

        var jwtResult = jwtAuthManager.GenerateTokens(request.UserName, claims, DateTime.Now);
        logger.LogInformation($"User [{request.UserName}] logged in the system.");
        return Ok(new LoginResult
        {
            UserName = request.UserName,
            Role = role,
            AccessToken = jwtResult.AccessToken,
            RefreshToken = jwtResult.RefreshToken.TokenString
        });
    }

    [HttpGet("user")]
    [Authorize]
    public ActionResult GetCurrentUser()
    {
        return Ok(new LoginResult
        {
            UserName = User.Identity?.Name!,
            Role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
            OriginalUserName = User.FindFirst("OriginalUserName")?.Value?? string.Empty
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public ActionResult Logout()
    {
        // optionally "revoke" JWT token on the server side --> add the current token to a block-list
        // https://github.com/auth0/node-jsonwebtoken/issues/375

        var userName = User.Identity?.Name!;
        jwtAuthManager.RemoveRefreshTokenByUserName(userName);
        logger.LogInformation("User [{userName}] logged out the system.", userName);
        return Ok();
    }

    [HttpPost("refresh-token")]
    [Authorize]
    public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var userName = User.Identity?.Name!;
            logger.LogInformation("User [{userName}] is trying to refresh JWT token.", userName);

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Unauthorized();
            }

            var accessToken = await HttpContext.GetTokenAsync("Bearer", "access_token");
            var jwtResult = jwtAuthManager.Refresh(request.RefreshToken, accessToken??string.Empty, DateTime.Now);
            logger.LogInformation("User [{userName}] has refreshed JWT token.", userName);
            return Ok(new LoginResult
            {
                UserName = userName,
                Role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
                AccessToken = jwtResult.AccessToken,
                RefreshToken = jwtResult.RefreshToken.TokenString
            });
        }
        catch (SecurityTokenException e)
        {
            return Unauthorized(e.Message); // return 401 so that the client side can redirect the user to login page
        }
    }

    [HttpPost("impersonation")]
    [Authorize(Roles = UserRoles.Admin)]
    public ActionResult Impersonate([FromBody] ImpersonationRequest request)
    {
        var userName = User.Identity?.Name!;
        logger.LogInformation("User [{userName}] is trying to impersonate [{anotherUserName}].", userName, request.UserName);

        var impersonatedRole = userService.GetUserRole(request.UserName);
        if (string.IsNullOrWhiteSpace(impersonatedRole))
        {
            logger.LogInformation("User [{userName}] failed to impersonate [{anotherUserName}] due to the target user not found.", userName, request.UserName);
            return BadRequest($"The target user [{request.UserName}] is not found.");
        }
        if (impersonatedRole == UserRoles.Admin)
        {
            logger.LogInformation("User [{userName}] is not allowed to impersonate another Admin.", userName);
            return BadRequest("This action is not supported.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name,request.UserName),
            new Claim(ClaimTypes.Role, impersonatedRole),
            new Claim("OriginalUserName", userName)
        };

        var jwtResult = jwtAuthManager.GenerateTokens(request.UserName, claims, DateTime.Now);
        logger.LogInformation("User [{request.UserName}] is impersonating [{anotherUserName}] in the system.", userName, request.UserName);
        return Ok(new LoginResult
        {
            UserName = request.UserName,
            Role = impersonatedRole,
            OriginalUserName = userName,
            AccessToken = jwtResult.AccessToken,
            RefreshToken = jwtResult.RefreshToken.TokenString
        });
    }

    [HttpPost("stop-impersonation")]
    [Authorize]
    public ActionResult StopImpersonation()
    {
        var userName = User.Identity?.Name!;
        var originalUserName = User.FindFirst("OriginalUserName")?.Value;
        if (string.IsNullOrWhiteSpace(originalUserName))
        {
            return BadRequest("You are not impersonating anyone.");
        }
        logger.LogInformation("User [{originalUserName}] is trying to stop impersonate [{userName}].",originalUserName, userName);

        var role = userService.GetUserRole(originalUserName);
        var claims = new[]
        {
            new Claim(ClaimTypes.Name,originalUserName),
            new Claim(ClaimTypes.Role, role)
        };

        var jwtResult = jwtAuthManager.GenerateTokens(originalUserName, claims, DateTime.Now);
        logger.LogInformation("User [{originalUserName}] has stopped impersonation.", originalUserName);
        return Ok(new LoginResult
        {
            UserName = originalUserName,
            Role = role,
            OriginalUserName = string.Empty,
            AccessToken = jwtResult.AccessToken,
            RefreshToken = jwtResult.RefreshToken.TokenString
        });
    }
}

public class LoginRequest
{
    [Required]
    [JsonPropertyName("username")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResult
{
    [JsonPropertyName("username")] public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;

    [JsonPropertyName("originalUserName")] public string OriginalUserName { get; set; } = string.Empty;

    [JsonPropertyName("accessToken")] public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")] public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [JsonPropertyName("refreshToken")] public string RefreshToken { get; set; } = string.Empty;
}

public class ImpersonationRequest
{
    [JsonPropertyName("username")] public string UserName { get; set; } = string.Empty;
}