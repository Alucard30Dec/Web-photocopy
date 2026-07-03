using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WebPhotocopyHub.Web.Diagnostics;

public static class CorrelationIdContext
{
    public const string HeaderName = "X-Correlation-ID";

    private const int MaxLength = 64;
    private const string ItemKey = "__WebPhotocopyHubCorrelationId";

    public static string GetOrCreate(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var stored) &&
            stored is string storedValue &&
            IsSafeValue(storedValue))
        {
            return storedValue;
        }

        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = IsSafeValue(incoming)
            ? incoming!.Trim()
            : CreateNew();

        context.Items[ItemKey] = correlationId;
        return correlationId;
    }

    public static bool IsSafeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            return false;
        }

        foreach (var character in trimmed)
        {
            if (character is >= 'a' and <= 'z')
            {
                continue;
            }

            if (character is >= 'A' and <= 'Z')
            {
                continue;
            }

            if (character is >= '0' and <= '9')
            {
                continue;
            }

            if (character is '-' or '_' or '.')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    public static bool IsApiRequest(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (context.GetEndpoint()?.Metadata.GetMetadata<ApiControllerAttribute>() is not null)
        {
            return true;
        }

        if (string.Equals(
                context.Request.Headers.XRequestedWith.FirstOrDefault(),
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return context.Request.Headers.Accept.Any(value =>
            value.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("text/json", StringComparison.OrdinalIgnoreCase));
    }

    private static string CreateNew()
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (IsSafeValue(traceId))
        {
            return traceId!;
        }

        return Guid.NewGuid().ToString("N");
    }
}
