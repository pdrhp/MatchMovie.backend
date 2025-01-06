namespace MatchMovie.Configuration;

public class RedisConfig
{
    public string ConnectionString { get; set; }
    public int DatabaseId { get; set; }
    public int DefaultExpirationInHours { get; set; } = 1;
}