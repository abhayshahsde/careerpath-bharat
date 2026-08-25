using MediatR;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CareerPath.Application.Common.Behaviors;

public sealed class HtmlSanitizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Regex XssRegex = new(
        @"<script[^>]*>[\s\S]*?</script>|&lt;script[^&gt;]*&gt;[\s\S]*?&lt;/script&gt;|</?script>|<[^>]+onload\s*=\s*['""]?[\s\S]*?['""]?[^>]*>|javascript\s*:",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<TResponse> Handle(TRequest req, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Intercept requests and sanitize string properties (Commands/Updates only, skip queries)
        var name = typeof(TRequest).Name;
        if (name.EndsWith("Command", StringComparison.OrdinalIgnoreCase) || name.EndsWith("Request", StringComparison.OrdinalIgnoreCase))
        {
            SanitizeObject(req);
        }

        return await next();
    }

    private static void SanitizeObject(object obj)
    {
        if (obj is null) return;

        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (prop.PropertyType == typeof(string) && prop.CanWrite && prop.CanRead)
            {
                var value = (string?)prop.GetValue(obj);
                if (!string.IsNullOrEmpty(value))
                {
                    var clean = SanitizeString(value);
                    if (clean != value)
                    {
                        prop.SetValue(obj, clean);
                    }
                }
            }
        }
    }

    private static string SanitizeString(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Clean out dangerous XSS tags
        var cleaned = XssRegex.Replace(input, string.Empty);
        
        // Strip general HTML tags to prevent XSS payloads in career titles / metadata uploads
        cleaned = Regex.Replace(cleaned, @"<[^>]*>", string.Empty);

        return cleaned.Trim();
    }
}
