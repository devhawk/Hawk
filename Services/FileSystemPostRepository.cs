using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Logging;

namespace Hawk
{    
    class FileSystemPostRepository : IPostRepository
    {
        const string ITEM_JSON = "hawk-post.json";
        const string COMMENTS_JSON = "hawk-comments.json";
        const string DASBLOG_COMPAT_JSON = "hawk-dasblog-compat.json";
        const string ITEM_CONTENT = "rendered-content.html";

        readonly string _path;
        ILogger _logger;

        Post[] _posts;
        Tuple<Category, int>[] _tags;
        Tuple<Category, int>[] _categories;
        readonly IMemoryCache _cache;
        readonly Dictionary<Guid, Post> _indexDasBlogEntryId = new Dictionary<Guid, Post>();
        readonly Dictionary<string, Post> _indexDasBlogTitle = new Dictionary<string, Post>();

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
            var key = title.ToLower();
            return _indexDasBlogTitle.ContainsKey(key) ? _indexDasBlogTitle[key] : null;
        }
        
        public Post PostByDasBlogTitle(string title, DateTimeOffset date)
        {
            var key = date.ToString("yyyy/MM/dd/") + title;
            return PostByDasBlogTitle(key);
        }

        static IEnumerable<Category> ConvertCsvCatString(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Enumerable.Empty<Category>();
            }
            
            return text.Split(',')
                .Select(s => s.Split('|'))
                .Select((string[] a) => new Category
                {
                    Title = a[0],
                    Slug = a[1],
                });
        }
        
        static PostAuthor ConvertPostAuthor(string author)
        {
            var a = author.Split('|');
            return new PostAuthor
            {
                Name = a[0],
                Slug = a[1],
                Email = a[2],
            };
        }

        static CommentAuthor ConvertCommentAuthor(JToken author)
        {
            return new CommentAuthor
            {
                Name = (string)author["author-name"],
                Email = (string)author["author-email"],
                Url = (string)author["author-url"],
            };
        }

        public FileSystemPostRepository(string path)
        {
            _path = path;
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public Task InitializeAsync(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(nameof(FileSystemPostRepository));
            _logger.LogInformation($"Loading data from from {_path}");

            var fsPosts = Directory
                .EnumerateDirectories(_path)
                .Where(dir => File.Exists(Path.Combine(dir, ITEM_JSON)))
                .Select(dir => new
                    {
                        Directory = dir,
                        Post = JObject.Parse(File.ReadAllText(Path.Combine(dir, ITEM_JSON))),
                    });
        
            var tempPosts = new List<Post>();
                
            foreach (var fsPost in fsPosts)
            {
                var post = new Post()
                {
                    Slug = (string)fsPost.Post["slug"],
                    Title = System.Net.WebUtility.HtmlDecode((string)fsPost.Post["title"]),
                    Date = DateTimeOffset.Parse((string)fsPost.Post["date"]),
                    DateModified = DateTimeOffset.Parse((string)fsPost.Post["modified"]),
                    Categories = ConvertCsvCatString((string)fsPost.Post["csv-category-slugs"]).ToList(),
                    Tags = ConvertCsvCatString((string)fsPost.Post["csv-tag-slugs"]).ToList(),
                    Author = ConvertPostAuthor((string)fsPost.Post["author"]),
                    CommentCount = int.Parse((string)fsPost.Post["comment-count"]),
                        
                    Content = () => _cache.AsyncMemoize(Path.Combine(fsPost.Directory, ITEM_CONTENT), key => Task.Run(() => File.ReadAllText(key))),
                    Comments = () => _cache.AsyncMemoize(Path.Combine(fsPost.Directory, COMMENTS_JSON), key => Task.Run(() => JArray
                        .Parse(File.ReadAllText(key))
                        .Select(fsc => new Comment
                            {
                                Content = (string)fsc["content"],
                                Date = DateTimeOffset.Parse((string)fsc["date"]),
                                Author = ConvertCommentAuthor(fsc), 
                            }))),
                };
    
                tempPosts.Add(post);
    
                Guid? dasBlogEntryId = fsPost.Post["dasblog-entry-id"] != null ? Guid.Parse((string)fsPost.Post["dasblog-entry-id"]) : (Guid?)null;
    
                if (dasBlogEntryId.HasValue)
                {
                    string dasBlogSlug = (string)fsPost.Post["dasblog-title"] ?? null;
                    string dasBlogUniqueSlug = (string)fsPost.Post["dasblog-unique-title"] ?? null;
    
                    _indexDasBlogEntryId[dasBlogEntryId.Value] = post;
                    _indexDasBlogTitle[dasBlogSlug.ToLower()] = post;
                    _indexDasBlogTitle[dasBlogUniqueSlug.ToLower()] = post;                    
                }
            }
    
            _posts = tempPosts.OrderByDescending(p => p.Date).ToArray();
                
            _tags = _posts
                .SelectMany(p => p.Tags)
                .GroupBy(c => c.Slug)
                .Select(g => Tuple.Create(g.First(), g.Count()))
                .ToArray();
                    
            _categories = _posts
                .SelectMany(p => p.Categories)
                .GroupBy(c => c.Slug)
                .Select(g => Tuple.Create(g.First(), g.Count()))
                .ToArray();
            
            return Task.Delay(0);    
        }
    }
}