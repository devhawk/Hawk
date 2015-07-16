using System;
using System.Threading.Tasks;
using Microsoft.Framework.Caching.Memory;

namespace HawkProto2
{
    static class MemoryCacheExtensions
    {
        public static async Task<TItem> AsyncMemoize<TItem>(this IMemoryCache cache, string key, Func<string, Task<TItem>> func)
        {
            TItem item;
            return cache.TryGetValue<TItem>(key, out item)
                ? item 
                : cache.Set<TItem>(key, await func(key));
        }
        
        public static TItem Memoize<TItem>(this IMemoryCache cache, string key, Func<string, TItem> func)
        {
            TItem item;
            return cache.TryGetValue<TItem>(key, out item)
                ? item 
                : cache.Set<TItem>(key, func(key));
        }
    }
}