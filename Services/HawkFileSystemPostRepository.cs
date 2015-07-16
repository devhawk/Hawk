using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Framework.Caching.Memory;

namespace HawkProto2
{    
    class HawkFileSystemPostRepository : IPostRepository
    {
        const string PATH = @"E:\dev\DevHawk\HawkContent";
        const string ITEM_JSON = "hawk-post.json";
        const string COMMENTS_JSON = "hawk-comments.json";
        const string DASBLOG_COMPAT_JSON = "hawk-dasblog-compat.json";
        const string ITEM_CONTENT = "rendered-content.html";
        
        Post[] _posts;
        Tuple<Category, int>[] _tags;
        Tuple<Category, int>[] _categories;
        IMemoryCache _cache;
        Dictionary<Guid, Post> _indexDasBlogEntryId = new Dictionary<Guid, Post>();
        Dictionary<string, Post> _indexDasBlogTitle = new Dictionary<string, Post>();
        
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

        static IEnumerable<BlogEntry> GetFSPosts()
        {
            return Directory
                .EnumerateDirectories(PATH)
                .Select(dir => {
                    var post = JsonConvert.DeserializeObject<FSPost>(File.ReadAllText(Path.Combine(dir, ITEM_JSON)));

                    var compatFilePath = Path.Combine(dir, DASBLOG_COMPAT_JSON);
                    var compat = File.Exists(compatFilePath) 
                        ? JsonConvert.DeserializeObject<FSDasBlogCompat>(File.ReadAllText(compatFilePath))
                        : null;
                        
                    return new BlogEntry
                    {
                        Directory = dir,
                        Post = post,
                        Compat = compat,
                    };
                });
        }

        HawkFileSystemPostRepository(IEnumerable<BlogEntry> blogEntries)
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
            var tempPosts = new List<Post>();
            
            foreach (var entry in blogEntries)
            {
                var post = new Post()
                {
                    Slug = entry.Post.Slug,
                    Title = System.Net.WebUtility.HtmlDecode(entry.Post.Title),
                    Date = entry.Post.Date,
                    DateModified = entry.Post.DateModified,
                    Categories = entry.Post.Categories.ToList(),
                    Tags = entry.Post.Tags.ToList(),
                    Author = entry.Post.Author,
                    CommentCount = entry.Post.CommentCount,

                    Content = () => _cache.Memoize(Path.Combine(entry.Directory, ITEM_CONTENT), key => Task.Run(() => File.ReadAllText(key))),
                    Comments = () => _cache.Memoize(Path.Combine(entry.Directory, COMMENTS_JSON), key => Task.Run(() => JsonConvert
                        .DeserializeObject<FSComment[]>(File.ReadAllText(key))
                        .Select(fsc => new Comment
                            {
                                Id = fsc.Id,
                                Content = fsc.Content,
                                Date = fsc.Date,
                                Author = fsc.Author, 
                            }))),
                };

                tempPosts.Add(post);
                
                if (entry.Compat != null)
                {
                    _indexDasBlogEntryId[entry.Compat.EntryId] = post;
                    _indexDasBlogTitle[entry.Compat.Title.ToLower()] = post;
                    _indexDasBlogTitle[entry.Compat.UniqueTitle.ToLower()] = post;
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
        }
        
        public static IPostRepository GetRepository()
        {
            return new HawkFileSystemPostRepository(GetFSPosts());
        }
        
        class BlogEntry
        {
            public string Directory { get; set; }
            public FSPost Post { get; set; }
            public FSDasBlogCompat Compat { get; set; }
        }
        
    	class FSDasBlogCompat
    	{
            [JsonProperty("entry-id")]
    	    public Guid EntryId { get; set; }
    	    [JsonProperty("title")]
    		public string Title { get; set; }
    	    [JsonProperty("unique-title")]
    		public string UniqueTitle { get; set; }
    	}
    	
    	class FSPost
    	{
            [JsonProperty("id")]
    	    public int Id { get; set; }
            [JsonProperty("slug")]
    	    public string Slug { get; set; }
            [JsonProperty("title")]
    	    public string Title { get; set; }
            [JsonProperty("date")]
    	    public DateTimeOffset Date { get; set; }
            [JsonProperty("modified")]
    	    public DateTimeOffset DateModified { get; set; }
            [JsonProperty("csv-category-slugs")]
    	    public string CsvCategorySlugs { get; set; }
            [JsonProperty("csv-tag-slugs")]
    	    public string CsvTagSlugs { get; set; }
            [JsonProperty("author")]
    	    public string InternalAuthor { get; set; }
            [JsonProperty("comment_count")]
    	    public int CommentCount { get; set; }
    		
    		private IEnumerable<Category> ConvertCsvCatString(string text)
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
         
            [JsonIgnore]   
            public IEnumerable<Category> Categories
            {
                get { return ConvertCsvCatString(CsvCategorySlugs); }
            }
    
            [JsonIgnore]   
            public IEnumerable<Category> Tags
            {
                get { return ConvertCsvCatString(CsvTagSlugs); }
            }
            
            [JsonIgnore]   
            public PostAuthor Author
            {
                get 
                {
                    var a = InternalAuthor.Split('|');
                    return new PostAuthor
                    {
                        Name = a[0],
                        Slug = a[1],
                        Email = a[2],
                    };
                } 
            }
    	}
        
        class FSComment
        {
            [JsonProperty("id")]
            public int Id { get; set; }
            [JsonProperty("author-name")]
            public string AuthorName { get; set; }
            [JsonProperty("author-email")]
            public string AuthorEmail { get; set; }
            [JsonProperty("author-url")]
            public string AuthorUrl { get; set; }
            [JsonProperty("date")]
            public DateTimeOffset Date { get; set; }
            [JsonProperty("Content")]
            public string Content { get; set; }
            [JsonProperty("parent-id")]
            public int ParentId { get; set; }
        
            [JsonIgnore]
            public CommentAuthor Author
            {
                get 
                {
                    return new CommentAuthor
                    {
                        Name = AuthorName,
                        Email = AuthorEmail,
                        Url = AuthorUrl,
                    };
                }
            }
        }

    }
}