using MatchMovie.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace MatchMovie.Controllers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRedisConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RedisConfig>(
            configuration.GetSection("Redis"));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<RedisConfig>>();
            return ConnectionMultiplexer.Connect(config.Value.ConnectionString);
        });

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
}