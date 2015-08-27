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
        const string POSTS = "MemoryCachePostRepository.Posts";
        const string TAGS = "MemoryCachePostRepository.Tags";
        const string CATEGORIES = "MemoryCachePostRepository.Categories";
        const string DASBLOG_ENTRYIDS = "MemoryCachePostRepository.DasBlogEntryIds";
        const string DASBLOG_TITLES = "MemoryCachePostRepository.DasBlogTitles";
        const string DASBLOG_UNIQUETITLES = "MemoryCachePostRepository.DasBlogUniqueTitles";

        Post[] _posts;
        Tuple<Category, int>[] _tags;
        Tuple<Category, int>[] _categories;
        Dictionary<Guid, Post> _indexDasBlogEntryId;
        Dictionary<string, Post> _indexDasBlogTitle;
        Dictionary<string, Post> _indexDasBlogUniqueTitle;

        public MemoryCachePostRepository(IMemoryCache cache)
        {
            _posts = cache.Get<Post[]>(POSTS);
            _tags = cache.Get<Tuple<Category, int>[]>(TAGS);
            _categories = cache.Get<Tuple<Category, int>[]>(CATEGORIES);
            _indexDasBlogEntryId = cache.Get<Dictionary<Guid, Post>>(DASBLOG_ENTRYIDS);
            _indexDasBlogTitle = cache.Get<Dictionary<string, Post>>(DASBLOG_TITLES);
            _indexDasBlogUniqueTitle = cache.Get<Dictionary<string, Post>>(DASBLOG_UNIQUETITLES);
        }

        static Func<Task<TItem>> MemoizeAsync<TItem>(IMemoryCache cache, string key, Func<Task<TItem>> func)
        {
            return async () =>
            {
                TItem item;
                return cache.TryGetValue<TItem>(key, out item)
                    ? item
                    : cache.Set<TItem>(key, await func(), new MemoryCacheEntryOptions() { SlidingExpiration = TimeSpan.FromMinutes(5) });
            };
        }

        public static void UpdateCache(IMemoryCache cache, IEnumerable<Post> posts)
        {
            var postArray = posts
                .Select(p =>
                {
                    // replace the Comments and Content function properties with versions that automatically cache the results
                    p.Comments = MemoizeAsync(cache, $"MemoryCachePostRepository.Post.{p.UniqueKey}.Comments", p.Comments);
                    p.Content = MemoizeAsync(cache, $"MemoryCachePostRepository.Post.{p.UniqueKey}.Content", p.Content);
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

        public Post PostByDasBlogEntryId(Guid entryId)
        {
            return _indexDasBlogEntryId.ContainsKey(entryId) ? _indexDasBlogEntryId[entryId] : null;
        }

        public Post PostByDasBlogTitle(string title)
        {
            return _indexDasBlogTitle[title.ToLowerInvariant()];
        }

        public Post PostByDasBlogTitle(string title, DateTimeOffset date)
        {
            var key = date.ToString("yyyy/MM/dd/") + title.ToLowerInvariant();
            return _indexDasBlogUniqueTitle[key];
        }
    }
}
