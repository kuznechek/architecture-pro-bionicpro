using System.Text.Json;
using System.Security.Cryptography;

namespace BionicProAuth.Middleware;

public class SessionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public SessionMiddleware(RequestDelegate next, ILogger<SessionMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var oldSessionId = context.Request.Cookies["session_id"];

        if (context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }

        if (string.IsNullOrEmpty(oldSessionId) || !SessionStore.Sessions.ContainsKey(oldSessionId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { Error = "Unauthorized" }));
            return;
        }

        var encryptedJson = SessionStore.Sessions[oldSessionId];
        var session = DecryptSession(encryptedJson);
        if (session == null || !session.TryGetValue("ExpiresAt", out var expiresAtObj) ||
            expiresAtObj is DateTime expiresAt && expiresAt < DateTime.UtcNow)
        {
            SessionStore.Sessions.Remove(oldSessionId);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { Error = "Session expired" }));
            return;
        }

        var newSessionId = GenerateSessionId();
        SessionStore.Sessions[newSessionId] = encryptedJson;
        SessionStore.Sessions.Remove(oldSessionId);

        var sessionTimeout = _configuration.GetValue<int>("Session:TimeoutMinutes", 60);
        context.Response.Cookies.Append("session_id", newSessionId, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromMinutes(sessionTimeout)
        });

        if (session.TryGetValue("AccessToken", out var accessTokenObj) && accessTokenObj is string accessToken)
        {
            context.Request.Headers.Append("X-Access-Token", accessToken);
        }

        await _next(context);
    }

    private string GenerateSessionId()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private Dictionary<string, object>? DecryptSession(string encryptedJson)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(encryptedJson);
        }
        catch
        {
            return null;
        }
    }
}

public static class SessionStore
{
    public static Dictionary<string, string> Sessions { get; set; } = new();
}
