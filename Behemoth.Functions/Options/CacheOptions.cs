using Microsoft.Extensions.Caching.Distributed;

namespace Behemoth.Functions.Options;

public class CacheOptions
{
    public int? ProfileExpirationInMinutes { get; set; }

    public DistributedCacheEntryOptions ProfileOptions => ProfileExpirationInMinutes.HasValue
        ? new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ProfileExpirationInMinutes.Value)
        }
        : new()
        {
            AbsoluteExpirationRelativeToNow = null
        };
    
    private const string Prefix = "cache_";

    public static string ProfileKey(string userId) => $"{Prefix}Profile:{userId}";
}