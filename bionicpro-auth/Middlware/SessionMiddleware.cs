using System.Text.Json;

namespace BionicProAuth.Middleware;

public class SessionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionMiddleware> _logger;

    public SessionMiddleware(RequestDelegate next, ILogger<SessionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sessionId = context.Request.Cookies["session_id"];
        
        if (context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }
        
        if (string.IsNullOrEmpty(sessionId) || !SessionStore.Sessions.ContainsKey(sessionId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { Error = "Unauthorized" }));
            return;
        }
        
        var session = SessionStore.Sessions[sessionId];
        var expiresAt = session.GetType().GetProperty("ExpiresAt")?.GetValue(session) as DateTime?;
        
        if (expiresAt.HasValue && expiresAt.Value < DateTime.UtcNow)
        {
            SessionStore.Sessions.Remove(sessionId);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { Error = "Session expired" }));
            return;
        }
        
        var accessToken = session.GetType().GetProperty("AccessToken")?.GetValue(session)?.ToString();
        if (!string.IsNullOrEmpty(accessToken))
        {
            context.Request.Headers.Add("X-Access-Token", accessToken);
        }
        
        await _next(context);
    }
}

public static class SessionStore
{
    public static Dictionary<string, string> Sessions { get; set; } = new();
}
