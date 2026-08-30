using System.Data.Common;
using Karry.Domain.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Karry.Infrastructure.Persistence;

/// <summary>
/// Sets the PostgreSQL session variable <c>app.current_tenant_id</c> on every opened
/// connection from the current tenant scope. Row-Level Security policies read this value to
/// restrict rows to the active tenant. <see cref="ITenantScoped"/> rows are also stamped on
/// insert via <see cref="KarryDbContext.ApplyTenantFiltering"/>.
/// </summary>
public sealed class RowLevelSecurityInterceptor : DbConnectionInterceptor
{
    private readonly ICurrentTenant _currentTenant;

    public RowLevelSecurityInterceptor(ICurrentTenant currentTenant)
    {
        _currentTenant = currentTenant;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetTenantContext((NpgsqlConnection)connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        SetTenantContext((NpgsqlConnection)connection);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private void SetTenantContext(NpgsqlConnection connection)
    {
        var tenantId = _currentTenant?.TenantId;
        if (tenantId is null)
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.current_tenant_id', @tenantId, false)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tenantId";
        parameter.Value = tenantId.Value.ToString();
        command.Parameters.Add(parameter);
        command.ExecuteNonQuery();
    }
}