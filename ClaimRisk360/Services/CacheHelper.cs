using Microsoft.Extensions.Caching.Memory;

namespace ClaimRisk360.Services;

/// <summary>
/// Utility for cache key generation and management
/// </summary>
public static class CacheKeys
{
    public const string AllClaims = "claims:all";
    public const string AllFraudRings = "fraudrings:all";
    public const string AllFraudRingsSummary = "fraudrings:summary";
    public const string DashboardStats = "dashboard:stats";
    public const string AllAuditEntries = "audit:all";
    public const string AllUsers = "users:all";
    public const string AllRoles = "roles:all";
    public const string ReferenceData = "referencedata:all";

    public static string FraudRing(string ringId) => $"fraudring:{ringId}";
    public static string Claim(string claimId) => $"claim:{claimId}";
    public static string User(string userId) => $"user:{userId}";
    public static string Role(string roleId) => $"role:{roleId}";
    public static string AuditEntry(string claimId) => $"audit:claim:{claimId}";
    public static string PatternAnalysis(string entityId) => $"pattern:entity:{entityId}";
}

/// <summary>
/// Cache duration settings for different data types
/// </summary>
public static class CacheDurations
{
    public static readonly TimeSpan ShortTerm = TimeSpan.FromMinutes(5);      // For frequently changing data
    public static readonly TimeSpan MediumTerm = TimeSpan.FromMinutes(30);    // For moderately stable data
    public static readonly TimeSpan LongTerm = TimeSpan.FromHours(1);         // For stable data
    public static readonly TimeSpan VeryLongTerm = TimeSpan.FromHours(4);    // For reference data
}

/// <summary>
/// Helper for managing cache with safe invalidation
/// </summary>
public class CacheHelper
{
    private readonly IMemoryCache _cache;

    public CacheHelper(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Get or create cache entry
    /// </summary>
    public T GetOrCreate<T>(string key, TimeSpan? duration, Func<T> factory) where T : class
    {
        if (_cache.TryGetValue(key, out T? cached))
            return cached!;

        var value = factory();
        if (value != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration ?? CacheDurations.MediumTerm
            };
            _cache.Set(key, value, cacheOptions);
        }

        return value!;
    }

    /// <summary>
    /// Get or create cache entry asynchronously
    /// </summary>
    public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan? duration, Func<Task<T>> factory) where T : class
    {
        if (_cache.TryGetValue(key, out T? cached))
            return cached!;

        var value = await factory();
        if (value != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration ?? CacheDurations.MediumTerm
            };
            _cache.Set(key, value, cacheOptions);
        }

        return value!;
    }

    /// <summary>
    /// Remove cache entry
    /// </summary>
    public void Remove(string key)
    {
        _cache.Remove(key);
    }

    /// <summary>
    /// Remove multiple related cache entries
    /// </summary>
    public void RemovePattern(params string[] keys)
    {
        foreach (var key in keys)
        {
            _cache.Remove(key);
        }
    }

    /// <summary>
    /// Clear all claim-related cache
    /// </summary>
    public void InvalidateClaimsCaches()
    {
        RemovePattern(
            CacheKeys.AllClaims,
            CacheKeys.DashboardStats,
            CacheKeys.AllFraudRings,
            CacheKeys.AllFraudRingsSummary
        );
    }

    /// <summary>
    /// Clear all audit-related cache
    /// </summary>
    public void InvalidateAuditCaches()
    {
        RemovePattern(CacheKeys.AllAuditEntries);
    }

    /// <summary>
    /// Clear all user-related cache
    /// </summary>
    public void InvalidateUserCaches()
    {
        RemovePattern(CacheKeys.AllUsers, CacheKeys.AllRoles);
    }
}
