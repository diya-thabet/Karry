using Karry.Domain.Common;
using Karry.Infrastructure.Context;
using Karry.Infrastructure.Persistence;
using Karry.Infrastructure.Persistence.Repositories;
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
        services.AddScoped<TenantContext>();

        services.AddDbContext<KarryDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.UseNetTopologySuite()));

        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<KarryDbContext>());

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:ConnectionString"];
        });

        return services;
    }
}