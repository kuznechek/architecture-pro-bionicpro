using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using BionicProAuth.Models;
using BionicProAuth.Services;
using BionicProAuth.Middleware;

namespace BionicProAuth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly KeycloakService _keycloakService;
    private readonly TokenProtector _tokenProtector;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        KeycloakService keycloakService,
        TokenProtector tokenProtector,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _keycloakService = keycloakService;
        _tokenProtector = tokenProtector;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var tokens = await _keycloakService.AuthenticateAsync(request.Username, request.Password);

            var userInfo = await _keycloakService.GetUserInfoAsync(tokens.AccessToken);

            var sessionId = GenerateSessionId();
            
            var sessionData = new
            {
                UserId = userInfo.Id,
                Username = userInfo.Username,
                Email = userInfo.Email,
                Roles = userInfo.Roles,
                AccessToken = _tokenProtector.Protect(tokens.AccessToken),
                RefreshToken = _tokenProtector.Protect(tokens.RefreshToken),
                ExpiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Session:TimeoutMinutes", 60))
            };
            
            var json = JsonSerializer.Serialize(sessionData);
            SessionStore.Sessions[sessionId] = json;
            
            
            Response.Cookies.Append("session_id", sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromMinutes(_configuration.GetValue<int>("Session:TimeoutMinutes", 60))
            });
            
            _logger.LogInformation("User {Username} logged in successfully", userInfo.Username);
            
            return Ok(new { Message = "Login successful", User = userInfo });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for user {Username}", request.Username);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var sessionId = Request.Cookies["session_id"];
        
        if (!string.IsNullOrEmpty(sessionId) && SessionStore.Sessions.TryGetValue(sessionId, out var encryptedJson))
        {
            var session = DecryptSession(encryptedJson);
            var refreshToken = session?.GetValueOrDefault("RefreshToken")?.ToString();
            
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _keycloakService.LogoutAsync(refreshToken);
            }
            
            SessionStore.Sessions.Remove(sessionId);
        }
        
        Response.Cookies.Delete("session_id");
        return Ok(new { Message = "Logged out successfully" });
    }

    [HttpGet("user")]
    public IActionResult GetCurrentUser()
    {
        var sessionId = Request.Cookies["session_id"];
        if (string.IsNullOrEmpty(sessionId) || !SessionStore.Sessions.TryGetValue(sessionId, out var encryptedJson))
            return Unauthorized(new { Error = "No active session" });
        
        var session = DecryptSession(encryptedJson);
        if (session == null)
            return Unauthorized(new { Error = "Invalid session data" });
        
        if (session.TryGetValue("ExpiresAt", out var expiresAtObj) &&
            expiresAtObj is DateTime expiresAt && expiresAt < DateTime.UtcNow)
        {
            SessionStore.Sessions.Remove(sessionId);
            return Unauthorized(new { Error = "Session expired" });
        }
        
        return Ok(new
        {
            UserId = session.GetValueOrDefault("UserId")?.ToString(),
            Username = session.GetValueOrDefault("Username")?.ToString(),
            Email = session.GetValueOrDefault("Email")?.ToString(),
            Roles = session.GetValueOrDefault("Roles") as List<string> ?? new List<string>()
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        var sessionId = Request.Cookies["session_id"];
        if (string.IsNullOrEmpty(sessionId) || !SessionStore.Sessions.TryGetValue(sessionId, out var encryptedJson))
            return Unauthorized(new { Error = "No active session" });
        
        var session = DecryptSession(encryptedJson);
        if (session == null)
            return Unauthorized(new { Error = "Invalid session data" });
        
        var refreshToken = session.GetValueOrDefault("RefreshToken")?.ToString();
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { Error = "No refresh token" });
        
        try
        {
            var newTokens = await _keycloakService.RefreshTokenAsync(refreshToken);
            var userInfo = await _keycloakService.GetUserInfoAsync(newTokens.AccessToken);
            
            var updatedSession = new
            {
                UserId = session.GetValueOrDefault("UserId")?.ToString() ?? "",
                Username = session.GetValueOrDefault("Username")?.ToString() ?? "",
                Email = session.GetValueOrDefault("Email")?.ToString() ?? "",
                Roles = session.GetValueOrDefault("Roles") as List<string> ?? new List<string>(),
                AccessTokenEncrypted = _tokenProtector.Protect(newTokens.AccessToken),
                RefreshTokenEncrypted = _tokenProtector.Protect(newTokens.RefreshToken),
                ExpiresAt = DateTime.UtcNow.AddSeconds(newTokens.ExpiresIn)
            };
            
            var json = JsonSerializer.Serialize(updatedSession);
            SessionStore.Sessions[sessionId] = json;
            
            return Ok(new { Message = "Token refreshed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed for session {SessionId}", sessionId);
            return Unauthorized(new { Error = "Token refresh failed" });
        }
    }

    [HttpGet("verify")]
    public IActionResult Verify()
    {
        var sessionId = Request.Cookies["session_id"];
        if (string.IsNullOrEmpty(sessionId) || !SessionStore.Sessions.ContainsKey(sessionId))
            return Unauthorized(new { Authenticated = false });
        
        return Ok(new { Authenticated = true });
    }

    private string GenerateSessionId()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
    
    private Dictionary<string, object>? DecryptSession(string encryptedJson)
    {
        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, object>>(encryptedJson);
            if (raw == null) return null;
            
            if (raw.TryGetValue("AccessTokenEncrypted", out var atEnc) && atEnc is string atEncStr)
                raw["AccessToken"] = _tokenProtector.Unprotect(atEncStr);
            if (raw.TryGetValue("RefreshTokenEncrypted", out var rtEnc) && rtEnc is string rtEncStr)
                raw["RefreshToken"] = _tokenProtector.Unprotect(rtEncStr);
            
            return raw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt session data");
            return null;
        }
    }
}
