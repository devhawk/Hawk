using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Caching.Memory;
using Hawk.Models;

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
        ILookup<string, Post> _indexDasBlogTitle;
        ILookup<string, Post> _indexDasBlogUniqueTitle;

        public MemoryCachePostRepository(IMemoryCache cache)
        {
            _posts = cache.Get<Post[]>(POSTS);
            _tags = cache.Get<Tuple<Category, int>[]>(TAGS);
            _categories = cache.Get<Tuple<Category, int>[]>(CATEGORIES);
            _indexDasBlogEntryId = cache.Get<Dictionary<Guid, Post>>(DASBLOG_ENTRYIDS);
            _indexDasBlogTitle = cache.Get<ILookup<string, Post>>(DASBLOG_TITLES);
            _indexDasBlogUniqueTitle = cache.Get<ILookup<string, Post>>(DASBLOG_UNIQUETITLES);
        }

        public static void UpdateCache(IMemoryCache cache, IEnumerable<Post> posts)
        {
            cache.Set(POSTS, posts.OrderByDescending(p => p.Date).ToArray());

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

            cache.Set<ILookup<string, Post>>(DASBLOG_TITLES, posts
                .Where(p => !string.IsNullOrEmpty(p.DasBlogTitle))
                .ToLookup(p => p.DasBlogTitle.ToLowerInvariant(), p => p));

            cache.Set<ILookup<string, Post>>(DASBLOG_UNIQUETITLES, posts
                .Where(p => !string.IsNullOrEmpty(p.DasBlogUniqueTitle))
                .ToLookup(p => p.DasBlogUniqueTitle.ToLowerInvariant(), p => p));
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
            return _indexDasBlogTitle[title.ToLowerInvariant()].FirstOrDefault();
        }

        public Post PostByDasBlogTitle(string title, DateTimeOffset date)
        {
            var key = date.ToString("yyyy/MM/dd/") + title.ToLowerInvariant();
            return _indexDasBlogTitle[key].FirstOrDefault();
        }
    }
}
