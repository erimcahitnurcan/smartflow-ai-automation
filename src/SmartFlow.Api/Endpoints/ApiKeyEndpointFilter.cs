using System.Security.Cryptography;
using System.Text;

namespace SmartFlow.Api.Endpoints;

/// <summary>
/// Allows a request only if the X-Api-Key header matches the configured key (SmartFlow:ApiKey).
/// Fails closed: when no key is configured, every request is rejected.
/// </summary>
public sealed class ApiKeyEndpointFilter(IConfiguration configuration) : IEndpointFilter
{
    public const string HeaderName = "X-Api-Key";
    public const string ConfigurationKey = "SmartFlow:ApiKey";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expectedKey = configuration[ConfigurationKey];
        var providedKey = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(expectedKey) || !KeysMatch(expectedKey, providedKey))
        {
            return TypedResults.Unauthorized();
        }

        return await next(context);
    }

    // Constant-time comparison, so response timing does not reveal how much of the key was correct.
    private static bool KeysMatch(string expected, string provided) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided));
}
