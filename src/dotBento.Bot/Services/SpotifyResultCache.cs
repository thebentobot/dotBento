using CSharpFunctionalExtensions;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Services;

/// <summary>Owns a small cache of selected results, separate from the application's shared cache.</summary>
public sealed class SpotifyResultCache : IDisposable
{
    internal const int Capacity = 512;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = Capacity });
    internal int Count => _cache.Count;

    public async Task<Maybe<T>> GetOrCreateAsync<T>(string key, Func<Task<Maybe<T>>> load) where T : class
    {
        if (_cache.TryGetValue(key, out T? cached))
            return cached.AsMaybe();

        var value = await load();
        if (value.HasNoValue)
            return Maybe<T>.None;

        if (_cache.Count >= Capacity)
            _cache.Compact(0.1);
        _cache.Set(key, value.Value, new MemoryCacheEntryOptions
        {
            Size = 1,
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
        });
        return value;
    }

    public void Dispose() => _cache.Dispose();
}
