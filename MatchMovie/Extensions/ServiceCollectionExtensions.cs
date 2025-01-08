using MatchMovie.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace MatchMovie.Controllers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRedisConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var logger = services.BuildServiceProvider().GetRequiredService<ILogger<IServiceCollection>>();
        
        var connectionString = configuration.GetSection("Redis").GetValue<string>("ConnectionString");
        logger.LogInformation("Configurando Redis com ConnectionString: {ConnectionString}", connectionString);

        services.Configure<RedisConfig>(
            configuration.GetSection("Redis"));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            logger.LogInformation("Iniciando conexão com Redis usando: {ConnectionString}", connectionString);
            return ConnectionMultiplexer.Connect(connectionString);
        });

        logger.LogInformation("Configuração do Redis concluída");
        return services;
    }

    public static IServiceCollection AddSignalRConfiguration(
        this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 102400;
        });

        return services;
    }

    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.SetIsOriginAllowed(_ => true)
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials();
            });
        });

        return services;
    }
}