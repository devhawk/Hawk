using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hawk
{
    public interface IPostRepository
    {
        Task InitializeAsync(ILoggerFactory loggerFactory);

        IEnumerable<Post> Posts();
        IEnumerable<Tuple<Category, int>> Tags();
        IEnumerable<Tuple<Category, int>> Categories();
            
        Post PostByDasBlogEntryId(Guid entryId);
        Post PostByDasBlogTitle(string title);
        Post PostByDasBlogTitle(string title, DateTimeOffset date);
    }

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
        Dictionary<Guid, Post> _indexDasBlogEntryId = new Dictionary<Guid, Post>();
        Dictionary<string, Post> _indexDasBlogTitle = new Dictionary<string, Post>();
        Dictionary<string, Post> _indexDasBlogUniqueTitle = new Dictionary<string, Post>();

        public MemoryCachePostRepository(IMemoryCache cache)
        {
            _posts = cache.Get<Post[]>(POSTS);
            _tags = cache.Get<Tuple<Category, int>[]>(TAGS);
            _categories= cache.Get<Tuple<Category, int>[]>(CATEGORIES);
            _indexDasBlogEntryId = cache.Get<Dictionary<Guid, Post>>(DASBLOG_ENTRYIDS);
            _indexDasBlogTitle= cache.Get<Dictionary<string, Post>>(DASBLOG_TITLES);
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

            cache.Set<Dictionary<string, Post>>(DASBLOG_TITLES, posts
                .Where(p => !string.IsNullOrEmpty(p.DasBlogTitle))
                .ToDictionary(p => p.DasBlogTitle.ToLowerInvariant(), p => p));

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

        public Task InitializeAsync(ILoggerFactory loggerFactory)
        {
            return Task.FromResult<object>(null);
        }

        public Post PostByDasBlogEntryId(Guid entryId)
        {
            return _indexDasBlogEntryId.ContainsKey(entryId) ? _indexDasBlogEntryId[entryId] : null;
        }

        public Post PostByDasBlogTitle(string title)
        {
            var key = title.ToLower();
            return _indexDasBlogTitle.ContainsKey(key) ? _indexDasBlogTitle[key] : null;
        }

        public Post PostByDasBlogTitle(string title, DateTimeOffset date)
        {
            var key = date.ToString("yyyy/MM/dd/") + title;
            return PostByDasBlogTitle(key);
        }
    }
}