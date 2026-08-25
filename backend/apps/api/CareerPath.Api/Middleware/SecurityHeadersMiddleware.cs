namespace CareerPath.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Prevent Clickjacking
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // 2. Prevent MIME-sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // 3. Prevent XSS executions in older browsers
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // 4. Tighten Referrer policy
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // 5. Basic Content-Security-Policy (Allow self, Swagger assets, scalar design files)
        context.Response.Headers.Append("Content-Security-Policy", 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "img-src 'self' data:; " +
            "frame-ancestors 'none';");

        await _next(context);
    }
}
