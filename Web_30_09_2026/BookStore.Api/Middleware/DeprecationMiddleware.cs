namespace BookStore.Api.Middleware;

public class DeprecationMiddleware
{
    private readonly RequestDelegate _next;

    public DeprecationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1"))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["Deprecation"] = "true";

                context.Response.Headers["Sunset"] =
                    "Wed, 31 Dec 2026 23:59:59 GMT";

                return Task.CompletedTask;
            });
        }

        await _next(context);
    }
}