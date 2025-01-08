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
        
        var redisUrl = configuration.GetSection("Redis").GetValue<string>("InternalUrl");
        var redisUri = new Uri(redisUrl);

        var host = redisUri.Host;
        var port = redisUri.Port;
        var connectionString = string.Empty;

        logger.LogInformation("Configurando Redis com URI: {RedisUri}", redisUri);
        logger.LogInformation("Host: {Host}, Port: {Port}", host, port);

        if (!string.IsNullOrEmpty(redisUri.UserInfo))
        {
            var password = redisUri.UserInfo.Split(':')[1];
            connectionString = $"{host}:{port},password={password},ssl=false,abortConnect=false";
            logger.LogInformation("Redis configurado com autenticação");
        }
        else
        {
            connectionString = $"{host}:{port},ssl=false,abortConnect=false";
            logger.LogInformation("Redis configurado sem autenticação");
        }

        services.Configure<RedisConfig>(
            configuration.GetSection("Redis"));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            logger.LogInformation("Iniciando conexão com Redis: {ConnectionString}", 
                connectionString.Replace(redisUri.UserInfo, "***")); // Oculta credenciais no log
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