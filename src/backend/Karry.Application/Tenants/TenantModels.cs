namespace Karry.Application.Tenants;

public sealed record CreateTenantRequest(
    string Name,
    string Country,
    string Currency,
    string Timezone = "UTC",
    string Locale = "en",
    string AdminEmail = "",
    string? AdminPassword = null,
    string? AdminName = null);

public sealed record CreateTenantResponse(Guid TenantId, string Name);