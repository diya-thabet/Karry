using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karry.MathEngine.Client;

public static class DependencyInjection
{
    public static IServiceCollection AddMathEngineClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MathEngineOptions>(configuration.GetSection(MathEngineOptions.SectionName));

        services.AddHttpClient<KarryMathEngineClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MathEngineOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        return services;
    }
}