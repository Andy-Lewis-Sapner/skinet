using System.Text.Json;
using Core.Interfaces;
using StackExchange.Redis;

namespace Infrastructure.Services;

public class ResponseCacheService(IConnectionMultiplexer redis) : IResponseCacheService
{
    private readonly IDatabase _database = redis.GetDatabase(1);
    private readonly JsonSerializerOptions _options =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task CacheResponseAsync(string cacheKey, object response, TimeSpan timeToLive)
    {
        string serializedResponse = JsonSerializer.Serialize(response, _options);
        await _database.StringSetAsync(cacheKey, serializedResponse, timeToLive);
    }

    public async Task<string?> GetCachedResponseAsync(string cacheKey)
    {
        RedisValue cachedReponse = await _database.StringGetAsync(cacheKey);
        if (cachedReponse.IsNullOrEmpty) return null;
        return cachedReponse;
    }

    public async Task RemoveCacheByPattern(string pattern)
    {
        IServer server = redis.GetServer(redis.GetEndPoints().First());
        RedisKey[] keys = [.. server.Keys(database: 1, pattern: $"*{pattern}*")];

        if (keys.Length != 0)
        {
            await _database.KeyDeleteAsync(keys);
        }
    }
}
