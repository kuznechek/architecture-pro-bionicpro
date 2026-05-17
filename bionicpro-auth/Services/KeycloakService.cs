using System.Text;
using System.Text.Json;
using BionicProAuth.Models;

namespace BionicProAuth.Services;

public class KeycloakService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakService> _logger;

    public KeycloakService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<KeycloakService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TokenResponse> AuthenticateAsync(string username, string password)
    {
        var tokenUrl = _configuration["Keycloak:TokenUrl"];
        
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _configuration["Keycloak:ClientId"]),
            new KeyValuePair<string, string>("client_secret", _configuration["Keycloak:ClientSecret"]),
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("scope", "openid")
        });

        var response = await _httpClient.PostAsync(tokenUrl, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Authentication failed: {Error}", error);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);
        
        return tokenResponse ?? throw new Exception("Failed to deserialize token response");
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        var tokenUrl = _configuration["Keycloak:TokenUrl"];
        
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _configuration["Keycloak:ClientId"]),
            new KeyValuePair<string, string>("client_secret", _configuration["Keycloak:ClientSecret"]),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var response = await _httpClient.PostAsync(tokenUrl, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Token refresh failed: {Error}", error);
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);
        
        return tokenResponse ?? throw new Exception("Failed to deserialize token response");
    }

    public async Task<UserInfo> GetUserInfoAsync(string accessToken)
    {
        var userInfoUrl = _configuration["Keycloak:Authority"] + "/protocol/openid-connect/userinfo";
        using var request = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("GetUserInfo failed: {Error}", error);
            throw new UnauthorizedAccessException("Failed to get user info");
        }
        
        var json = await response.Content.ReadAsStringAsync();
        var userInfoRaw = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        
        return new UserInfo
        {
            Id = userInfoRaw?.GetValueOrDefault("sub")?.ToString() ?? "",
            Username = userInfoRaw?.GetValueOrDefault("preferred_username")?.ToString() ?? "",
            Email = userInfoRaw?.GetValueOrDefault("email")?.ToString() ?? "",
            Roles = ExtractRoles(userInfoRaw)
        };
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var logoutUrl = _configuration["Keycloak:LogoutUrl"];
        
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _configuration["Keycloak:ClientId"]),
            new KeyValuePair<string, string>("client_secret", _configuration["Keycloak:ClientSecret"]),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        await _httpClient.PostAsync(logoutUrl, content);
    }

    private List<string> ExtractRoles(Dictionary<string, object>? userInfo)
    {
        var roles = new List<string>();
        
        if (userInfo?.TryGetValue("realm_access", out var realmAccessObj) == true)
        {
            var realmAccess = JsonSerializer.Serialize(realmAccessObj);
            var realmDict = JsonSerializer.Deserialize<Dictionary<string, object>>(realmAccess);
            
            if (realmDict?.TryGetValue("roles", out var rolesObj) == true)
            {
                var rolesList = JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(rolesObj));
                if (rolesList != null) roles.AddRange(rolesList);
            }
        }
        
        return roles;
    }
}
