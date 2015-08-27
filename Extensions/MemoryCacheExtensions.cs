using System;
using System.Threading.Tasks;
using Microsoft.Framework.Caching.Memory;

namespace Hawk
{
    static class MemoryCacheExtensions
    {
        public static async Task<TItem> MemoizeAsync<TItem>(this IMemoryCache cache, string key, Func<string, Task<TItem>> func)
        {
            TItem item;
            return cache.TryGetValue<TItem>(key, out item)
                ? item 
                : cache.Set<TItem>(key, await func(key), new MemoryCacheEntryOptions() { SlidingExpiration = TimeSpan.FromMinutes(5)});
        }
        
        public static TItem Memoize<TItem>(this IMemoryCache cache, string key, Func<string, TItem> func)
        {
            TItem item;
            return cache.TryGetValue<TItem>(key, out item)
                ? item 
                : cache.Set<TItem>(key, func(key), new MemoryCacheEntryOptions() { SlidingExpiration = TimeSpan.FromMinutes(5)});
        }
    }
}