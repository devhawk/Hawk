using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Caching.Memory;
using Hawk.Models;
using System.Threading.Tasks;

namespace Hawk.Services
{
    public class MemoryCachePostRepository : IPostRepository
    {
        const string CLASS_NAME = nameof(MemoryCachePostRepository);

        const string POSTS = CLASS_NAME + ".Posts";
        const string TAGS = CLASS_NAME + ".Tags";
        const string CATEGORIES = CLASS_NAME + ".Categories";
        const string DASBLOG_ENTRYIDS = CLASS_NAME + ".DasBlogEntryIds";
        const string DASBLOG_TITLES = CLASS_NAME + ".DasBlogTitles";
        const string DASBLOG_UNIQUETITLES = CLASS_NAME + ".DasBlogUniqueTitles";

        IMemoryCache _cache;

        Post[] _posts;
        Tuple<Category, int>[] _tags;
        Tuple<Category, int>[] _categories;

        public MemoryCachePostRepository(IMemoryCache cache)
        {
            _cache = cache;

            // these collections are used on almost every request, so grab them from cache right away
            _posts = _cache.Get<Post[]>(POSTS);
            _tags = _cache.Get<Tuple<Category, int>[]>(TAGS);
            _categories = _cache.Get<Tuple<Category, int>[]>(CATEGORIES);
        }

        static Func<Task<TItem>> MemoizeAsync<TItem>(IMemoryCache cache, string key, Func<Task<TItem>> func, MemoryCacheEntryOptions options)
        {
            return async () =>
            {
                TItem item;
                return cache.TryGetValue<TItem>(key, out item)
                    ? item
                    : cache.Set<TItem>(key, await func(), options);
            };
        }

        static Func<Task<TItem>> MemoizeAsync<TItem>(IMemoryCache cache, string key, Func<Task<TItem>> func)
        {
            return MemoizeAsync(cache, key, func, new MemoryCacheEntryOptions() { SlidingExpiration = TimeSpan.FromMinutes(5) });
        }

        public static void UpdateCache(IMemoryCache cache, IEnumerable<Post> posts)
        {
            var postArray = posts
                .Select(p =>
                {
                    // replace the Comments and Content function properties with versions that automatically cache the results
                    p.Comments = MemoizeAsync(cache, $"{CLASS_NAME}.Post.{p.UniqueKey}.Comments", p.Comments);
                    p.Content = MemoizeAsync(cache, $"{CLASS_NAME}.Post.{p.UniqueKey}.Content", p.Content);
                    return p;
                })
                .OrderByDescending(p => p.Date)
                .ToArray();

            cache.Set(POSTS, postArray);

            cache.Set(TAGS, posts
                .SelectMany(p => p.Tags)
                .GroupBy(c => c.Slug)
                .Select(g => Tuple.Create(g.First(), g.Count()))
                .ToArray());

            cache.Set(CATEGORIES, posts
                .SelectMany(p => p.Categories)
                .GroupBy(c => c.Slug)
                .Select(g => Tuple.Create(g.First(), g.Count()))
                .ToArray());

            cache.Set<Dictionary<Guid, Post>>(DASBLOG_ENTRYIDS, posts
                .Where(p => p.DasBlogEntryId.HasValue)
                .ToDictionary(p => p.DasBlogEntryId.Value, p => p));

            var lookupDasBlogTitle = posts
                .Where(p => !string.IsNullOrEmpty(p.DasBlogTitle))
                .ToLookup(p => p.DasBlogTitle.ToLowerInvariant(), p => p);

            cache.Set<Dictionary<string, Post>>(DASBLOG_TITLES, lookupDasBlogTitle
                .ToDictionary(g => g.Key, g => g.First()));

            cache.Set<Dictionary<string, Post>>(DASBLOG_UNIQUETITLES, posts
                .Where(p => !string.IsNullOrEmpty(p.DasBlogUniqueTitle))
                .ToDictionary(p => p.DasBlogUniqueTitle.ToLowerInvariant(), p => p));
        }

        public IEnumerable<Post> Posts()
        {
            return _posts;
        }

        public IEnumerable<Tuple<Category, int>> Tags()
        {
            return _tags;
        }

        public IEnumerable<Tuple<Category, int>> Categories()
        {
            return _categories;
        }

        // the dasblog related collections are used rarely, if ever, so only retrieve them from cache on demand

        Post GetFromCachedIndex<T>(string indexName, T key)
        {
            var index = _cache.Get<Dictionary<T, Post>>(indexName);
            return index.ContainsKey(key) ? index[key] : null;
        }

        public Post PostByDasBlogEntryId(Guid entryId)
        {
            return GetFromCachedIndex(DASBLOG_ENTRYIDS, entryId);
        }

        public Post PostByDasBlogTitle(string title)
        {
            var key = title.ToLowerInvariant();
            return GetFromCachedIndex(DASBLOG_TITLES, key);
        }

        public Post PostByDasBlogTitle(string title, DateTimeOffset date)
        {
            var key = date.ToString("yyyy/MM/dd/") + title.ToLowerInvariant();
            return GetFromCachedIndex(DASBLOG_UNIQUETITLES, key);
        }
    }
}
