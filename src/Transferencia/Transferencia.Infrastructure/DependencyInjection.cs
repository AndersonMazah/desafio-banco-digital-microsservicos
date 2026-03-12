using Transferencia.Domain.Interfaces;
using Transferencia.Infrastructure.Clients;
using Transferencia.Infrastructure.Data;
using Transferencia.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Transferencia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PostgresOptions>(configuration.GetSection(PostgresOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services
            .AddOptions<ContaCorrenteApiOptions>()
            .Bind(configuration.GetSection(ContaCorrenteApiOptions.SectionName))
            .PostConfigure(options =>
            {
                var servicesBaseUrl = configuration[ContaCorrenteApiOptions.ServicesBaseUrlKey];
                if (!string.IsNullOrWhiteSpace(servicesBaseUrl))
                {
                    options.BaseUrl = servicesBaseUrl;
                }
            });

        services.AddScoped<IUnitOfWork, DapperUnitOfWork>();
        services.AddScoped<IContaCorrenteRepository, ContaCorrenteRepository>();
        services.AddScoped<IIdempotenciaRepository, IdempotenciaRepository>();
        services.AddScoped<ITransferenciaRepository, TransferenciaRepository>();

        services.AddHttpClient<IContaCorrenteClient, ContaCorrenteClient>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<ContaCorrenteApiOptions>>()
                .Value;

            client.BaseAddress = new Uri(options.BaseUrl);
        });
        return services;
    }

}
