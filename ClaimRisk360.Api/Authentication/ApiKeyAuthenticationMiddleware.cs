using System.Security.Claims;

namespace ClaimRisk360.Api.Authentication;

/// <summary>
/// API Key authentication handler using a strong key from configuration.
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private const string ApiKeyHeaderName = "X-Api-Key";

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health endpoint and SignalR hub negotiation
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/hubs"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key is required. Provide it via the X-Api-Key header." });
            return;
        }

        var configuredApiKey = _configuration["ApiKey:Secret"];
        if (string.IsNullOrEmpty(configuredApiKey) || !string.Equals(extractedApiKey, configuredApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key." });
            return;
        }

        // Set a basic identity for authorized requests
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "ApiClient"),
            new Claim("AuthMethod", "ApiKey")
        ], "ApiKey");
        context.User = new ClaimsPrincipal(identity);

        await _next(context);
    }
}

public static class ApiKeyAuthenticationExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }
}
