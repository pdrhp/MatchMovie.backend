using MatchMovie.Configuration;
using MatchMovie.Interfaces;
using MatchMovie.Models.Entities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace MatchMovie.Infrastructure.Persistence.Redis;

public class RedisConnectionMapping : IConnectionMapping
{
    
    private readonly IConnectionMultiplexer _redis;
    private readonly IOptions<RedisConfig> _redisConfig;
    private const string KeyPrefix = "room:";
    
    public RedisConnectionMapping(
        IConnectionMultiplexer redis,
        IOptions<RedisConfig> redisConfig)
    {
        _redis = redis;
        _redisConfig = redisConfig;
    }
    
    public async Task AddRoom(Room room)
    {
        var db = _redis.GetDatabase();
        var serializedRoom = JsonConvert.SerializeObject(room);
        
        await db.StringSetAsync(
            $"{KeyPrefix}{room.Code}",
            serializedRoom,
            TimeSpan.FromHours(_redisConfig.Value.DefaultExpirationInHours)
        );
    }

    public async Task<Room> GetRoom(string code)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync($"{KeyPrefix}{code}");
        
        if (value.IsNull)
            return null;

        try
        {
            return JsonConvert.DeserializeObject<Room>(value.ToString());
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Erro ao deserializar sala", ex);
        }
    }

    public async Task UpdateRoom(Room room)
    {
        await AddRoom(room);
    }

    public async Task RemoveRoom(string code)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"{KeyPrefix}{code}");
    }

    public async Task<IEnumerable<string>> GetAllRoomCodes()
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: $"{KeyPrefix}*");
        
        return keys.Select(k => k.ToString().Replace(KeyPrefix, ""));
    }
}