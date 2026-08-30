namespace Karry.Api.Middleware;

/// <summary>
/// Exposes the tenant id claim from the authenticated principal to the ambient request
/// context. The Infrastructure layer's connection interceptor reads this value and sets
/// <c>app.current_tenant_id</c> on the PostgreSQL session for Row-Level Security.
/// </summary>
public sealed class TenantContextMiddleware
{
    private const string CurrentTenantKey = "CurrentTenantId";

    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.User.FindFirst("tenant_id")?.Value;

        if (!string.IsNullOrEmpty(tenantId))
        {
            context.Items[CurrentTenantKey] = tenantId;
        }

        await _next(context);
    }
}