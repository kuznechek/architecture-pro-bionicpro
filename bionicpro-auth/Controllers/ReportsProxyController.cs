using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BionicProAuth.Models;
using BionicProAuth.Services;
using BionicProAuth.Middleware;

namespace bionicpro_auth.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsProxyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ReportsProxyController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("{**path}")]
        public async Task<IActionResult> Proxy(string path, [FromQuery] string from_date, [FromQuery] string to_date, [FromQuery] string format = "json")
        {
            var sessionId = Request.Cookies["session_id"];
            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized("Missing session");

            var userId = GetUserIdFromSession(sessionId);
            if (userId == null)
                return Unauthorized("User not found in session");

            var url = $"http://report-service:8080/reports/{path}?from_date={from_date}&to_date={to_date}&format={format}";
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-User-Id", userId);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

            if (format == "pdf")
            {
                var stream = await response.Content.ReadAsStreamAsync();
                return File(stream, "application/pdf", $"report_{from_date}_{to_date}.pdf");
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
        }

        private string GetUserIdFromSession(string sessionId)
        {
            if (SessionStore.Sessions.TryGetValue(sessionId, out var json))
            {
                var session = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                return session?.GetValueOrDefault("UserId")?.ToString();
            }
            return null;
        }
    }
}