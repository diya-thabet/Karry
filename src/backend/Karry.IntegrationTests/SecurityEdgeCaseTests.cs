using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Karry.IntegrationTests;

public sealed class SecurityEdgeCaseTests : IClassFixture<KarryApiFactory>
{
    private readonly KarryApiFactory _factory;

    public SecurityEdgeCaseTests(KarryApiFactory factory)
    {
        _factory = factory;
    }

    private Task<HttpClient> NewClientAsync()
    {
        if (!KarryApiFactory.IsPostgresConfigured)
        {
            throw new SkipException("PostgreSQL is not configured; integration tests run only in CI.");
        }

        return Task.FromResult(_factory.CreateClient());
    }

    private static void Auth(HttpClient client, string accessToken)
        => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    private static async Task<LoginResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password, "dev"));
        var body = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        return body!;
    }

    [Fact]
    public async Task FiveWrongPasswords_ThenValidPassword_LockedOut()
    {
        var client = await NewClientAsync();

        var adminEmail = Environment.GetEnvironmentVariable("Seed__AdminEmail") ?? "root@kar.app";
        var adminPassword = Environment.GetEnvironmentVariable("Seed__AdminPassword") ?? "Karry#RootAdmin1";
        var adminLogin = await LoginAsync(client, adminEmail, adminPassword);
        adminLogin.Tokens.Should().NotBeNull();
        Auth(client, adminLogin.Tokens!.AccessToken);

        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantAdminEmail = $"lockadmin-{unique}@kar.app";
        const string tenantAdminPassword = "Karry#Lock1";
        var tenantResp = await client.PostAsJsonAsync("/api/tenants", new
        {
            name = $"Lock-Tenant-{unique}",
            country = "KE",
            currency = "USD",
            timezone = "UTC",
            locale = "en",
            adminEmail = tenantAdminEmail,
            adminPassword = tenantAdminPassword,
            adminName = "Lock Admin",
        });
        tenantResp.StatusCode.Should().Be(HttpStatusCode.Created);

        for (var i = 0; i < 5; i++)
        {
            var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(tenantAdminEmail, "WrongPass!1", "dev"));
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var locked = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(tenantAdminEmail, tenantAdminPassword, "dev"));
        locked.StatusCode.Should().Be(HttpStatusCode.Locked);
    }

    [Fact]
    public async Task TenantIsolation_OtherTenantsUsersNotVisible()
    {
        var client = await NewClientAsync();

        var adminEmail = Environment.GetEnvironmentVariable("Seed__AdminEmail") ?? "root@kar.app";
        var adminPassword = Environment.GetEnvironmentVariable("Seed__AdminPassword") ?? "Karry#RootAdmin1";
        var adminLogin = await LoginAsync(client, adminEmail, adminPassword);
        Auth(client, adminLogin.Tokens!.AccessToken);

        // Tenant A admin.
        var ua = Guid.NewGuid().ToString("N")[..8];
        var aEmail = $"isoA-{ua}@kar.app";
        var aResp = await client.PostAsJsonAsync("/api/tenants", new
        {
            name = $"IsoA-{ua}",
            country = "KE",
            currency = "USD",
            timezone = "UTC",
            locale = "en",
            adminEmail = aEmail,
            adminPassword = "Karry#IsoA1",
            adminName = "A",
        });
        aResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Tenant B admin.
        var ub = Guid.NewGuid().ToString("N")[..8];
        var bEmail = $"isoB-{ub}@kar.app";
        var bResp = await client.PostAsJsonAsync("/api/tenants", new
        {
            name = $"IsoB-{ub}",
            country = "KE",
            currency = "USD",
            timezone = "UTC",
            locale = "en",
            adminEmail = bEmail,
            adminPassword = "Karry#IsoB1",
            adminName = "B",
        });
        bResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Tenant B admin lists users: must not see tenant A's admin (RLS isolation).
        var bLogin = await LoginAsync(client, bEmail, "Karry#IsoB1");
        Auth(client, bLogin.Tokens!.AccessToken);

        var usersB = await client.GetFromJsonAsync<UserResponse[]>("/api/users");
        usersB.Should().NotBeNull();
        usersB!.Select(u => u.Email).Should().NotContain(aEmail);
    }
}