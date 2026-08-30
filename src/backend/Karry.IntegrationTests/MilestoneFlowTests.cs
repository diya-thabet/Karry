using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Karry.IntegrationTests;

public sealed class MilestoneFlowTests : IClassFixture<KarryApiFactory>
{
    private readonly KarryApiFactory _factory;

    public MilestoneFlowTests(KarryApiFactory factory)
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

    private static async Task<LoginResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password, "test-device"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.RequiresTwoFactor.Should().BeFalse();
        body.Tokens.Should().NotBeNull();
        return body!;
    }

    private static void Auth(HttpClient client, string accessToken)
        => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    [Fact]
    public async Task FullMilestone_AdminCreatesTenant_OperatorLogsIn_UnitsConvertBothWays()
    {
        var client = await NewClientAsync();

        // 1. Platform admin (seeded) logs in.
        var adminEmail = Environment.GetEnvironmentVariable("Seed__AdminEmail") ?? "root@kar.app";
        var adminPassword = Environment.GetEnvironmentVariable("Seed__AdminPassword") ?? "Karry#RootAdmin1";
        var adminLogin = await LoginAsync(client, adminEmail, adminPassword);

        // 2. Admin creates a tenant, which provisions the six roles + an admin.
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantName = $"Quarry-{unique}";
        var tenantAdminEmail = $"admin-{unique}@kar.app";
        const string tenantAdminPassword = "Karry#Tenant1";

        Auth(client, adminLogin.Tokens!.AccessToken);
        var tenantResponse = await client.PostAsJsonAsync("/api/tenants", new
        {
            name = tenantName,
            country = "KE",
            currency = "USD",
            timezone = "UTC",
            locale = "en",
            adminEmail = tenantAdminEmail,
            adminPassword = tenantAdminPassword,
            adminName = "Tenant Chief",
        });
        tenantResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdTenant = await tenantResponse.Content.ReadFromJsonAsync<CreateTenantResponse>();
        createdTenant!.Name.Should().Be(tenantName);

        // 3. Tenant admin logs in and provisions an operator with the operator role.
        var tenantAdminLogin = await LoginAsync(client, tenantAdminEmail, tenantAdminPassword);
        Auth(client, tenantAdminLogin.Tokens!.AccessToken);

        var roles = await client.GetFromJsonAsync<RoleResponse[]>("/api/roles");
        roles.Should().NotBeNullOrEmpty();
        var operatorRole = roles!.Single(r => r.Code == "operator");

        var operatorEmail = $"op-{Guid.NewGuid().ToString("N")[..8]}@kar.app";
        var operatorResponse = await client.PostAsJsonAsync("/api/users", new
        {
            email = operatorEmail,
            name = "Olaf",
            password = "Karry#Op123",
            roleId = operatorRole.RoleId,
        });
        operatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Operator logs in and converts units both ways.
        var operatorLogin = await LoginAsync(client, operatorEmail, "Karry#Op123");
        Auth(client, operatorLogin.Tokens!.AccessToken);

        var convertResponse = await client.PostAsJsonAsync("/api/units/convert", new
        {
            fromUnit = "m3",
            value = 10,
            rhoDryTonPerM3 = 1.6,
            kappaMoisture = 1.1,
        });
        convertResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var m3ToT = await convertResponse.Content.ReadFromJsonAsync<ConvertResponse>();
        m3ToT!.Value.Should().BeGreaterThan(0);
        m3ToT.ToUnit.Should().Be("t");

        var convertBack = await client.PostAsJsonAsync("/api/units/convert", new
        {
            fromUnit = "t",
            value = Convert.ToDecimal(m3ToT.Value),
            rhoDryTonPerM3 = 1.6,
            kappaMoisture = 1.1,
        });
        convertBack.StatusCode.Should().Be(HttpStatusCode.OK);
        var tToM3 = await convertBack.Content.ReadFromJsonAsync<ConvertResponse>();
        tToM3!.Value.Should().BeApproximately(10m, 0.02m);
        tToM3.ToUnit.Should().Be("m3");
    }
}