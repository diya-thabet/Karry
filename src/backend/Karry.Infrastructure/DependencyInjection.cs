using Karry.Application.Auth;
using Karry.Application.Common;
using Karry.Application.Security;
using Karry.Domain.Common;
using Karry.Infrastructure.Auth;
using Karry.Infrastructure.Context;
using Karry.Infrastructure.Persistence;
using Karry.Infrastructure.Persistence.Repositories;
using Karry.Infrastructure.Security;
using Karry.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karry.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("KarryDatabase")
            ?? throw new InvalidOperationException("Connection string 'KarryDatabase' is not configured.");

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ICurrentSession>(sp => sp.GetRequiredService<CurrentSessionProvider>());
        services.AddScoped<TenantContext>();
        services.AddScoped<CurrentSessionProvider>();

        services.AddDbContext<KarryDbContext>((sp, options) =>
            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.UseNetTopologySuite())
                .AddInterceptors(sp.GetRequiredService<RowLevelSecurityInterceptor>()));

        services.AddScoped<RowLevelSecurityInterceptor>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<KarryDbContext>());
        services.AddScoped<DbSeeder>();

        services.AddScoped<IClock, SystemClock>();
        services.AddScoped<ISecureRandom, SecureRandom>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITotpService, TotpService>();
        services.AddScoped<ITokenIssuer, TokenIssuer>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:ConnectionString"];
        });

        return services;
    }
}

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}