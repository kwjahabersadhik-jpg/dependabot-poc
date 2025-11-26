using Newtonsoft.Json;

namespace CreditCardsSystem.Application;

public static class CacheExtension
{
    /// <summary>
    /// Asynchronously gets a string from the specified cache with the specified key.
    /// </summary>
    /// <param name="cache">The cache in which to store the data.</param>
    /// <param name="key">The key to get the stored data for.</param>
    /// <param name="token">Optional. A <see cref="CancellationToken" /> to cancel the operation.</param>
    /// <returns>A task that gets the string value from the stored cache key.</returns>
    public static async Task<T?> GetJsonAsync<T>(this IDistributedCache cache, string key, CancellationToken token = default)
    {
        var data = await cache.GetAsync(key, token).ConfigureAwait(false);
        return data == null ? default : JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data));
    }

    /// <summary>
    /// Asynchronously sets a string in the specified cache with the specified key.
    /// </summary>
    /// <param name="cache">The cache in which to store the data.</param>
    /// <param name="key">The key to store the data in.</param>
    /// <param name="value">The data to store in the cache.</param>
    /// <param name="options">The cache options for the entry.</param>
    /// <param name="token">Optional. A <see cref="CancellationToken" /> to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous set operation.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null.</exception>
    public static async Task SetJsonAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options, CancellationToken token = default) where T : class
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var json = JsonConvert.SerializeObject(value);
        var bytes = Encoding.UTF8.GetBytes(json);

        await cache.SetAsync(key, bytes, options, token);
    }

    /// <summary>
    /// Asynchronously sets a string in the specified cache with the specified key.
    /// </summary>
    /// <param name="cache">The cache in which to store the data.</param>
    /// <param name="key">The key to store the data in.</param>
    /// <param name="value">The data to store in the cache.</param>
    /// <param name="expiryTimeInMinutes">The cache expiration time (in minutes) for the entry.</param>
    /// <param name="token">Optional. A <see cref="CancellationToken" /> to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous set operation.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null.</exception>
    public static async Task SetJsonAsync<T>(this IDistributedCache cache, string key, T value, int expiryTimeInMinutes, CancellationToken token = default) where T : class
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var json = JsonConvert.SerializeObject(value);
        var bytes = Encoding.UTF8.GetBytes(json);

        if (expiryTimeInMinutes > 0)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiryTimeInMinutes)
            };

            await cache.SetAsync(key, bytes, options, token);
        }
        else
        {
            await cache.SetAsync(key, bytes, token);
        }
    }
}