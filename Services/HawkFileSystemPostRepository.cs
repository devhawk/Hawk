using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Framework.Caching.Memory;

namespace HawkProto2
{
	public class FSDasBlogCompat
	{
        [JsonProperty("entry-id")]
	    public Guid EntryId { get; set; }
	    [JsonProperty("slug")]
		public string Slug { get; set; }
	    [JsonProperty("unique-slug")]
		public string UniqueSlug { get; set; }
	}
	
	public class FSPost
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
    
    public class FSComment
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
    
    class HawkFileSystemPostRepository : IPostRepository
    {
        const string PATH = @"E:\dev\DevHawk\HawkContent";
        const string ITEM_JSON = "hawk-post.json";
        const string COMMENTS_JSON = "hawk-comments.json";
        const string DASBLOG_COMPAT_JSON = "hawk-dasblog-compat.json";
        const string ITEM_CONTENT = "rendered-content.html";
        
        static Post[] _posts = null;
        static Tuple<Category, int>[] _tags = null;
        static Tuple<Category, int>[] _categories = null;
        static Dictionary<Guid, Post> _indexDasBlogEntryId = new Dictionary<Guid, Post>();
        static Dictionary<string, Post> _indexDasBlogTitle = new Dictionary<string, Post>();
        static IMemoryCache _cache = null;

        static TItem Memoize<TItem>(IMemoryCache cache, string key, Func<string, TItem> func)
        {
            TItem item;
            return cache.TryGetValue<TItem>(key, out item)
                ? item 
                : cache.Set<TItem>(key, func(key));
        }

        static HawkFileSystemPostRepository()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());

            var tempPosts = new List<Post>();

            foreach (var dir in Directory.EnumerateDirectories(PATH))
            {
                var jsonItemText = File.ReadAllText(Path.Combine(dir, ITEM_JSON));
                var jsonItem = JsonConvert.DeserializeObject<FSPost>(jsonItemText);
                
                var post = new Post()
                {
                    Slug = jsonItem.Slug,
                    Title = System.Net.WebUtility.HtmlDecode(jsonItem.Title),
                    Date = jsonItem.Date,
                    DateModified = jsonItem.DateModified,
                    Categories = jsonItem.Categories.ToList(),
                    Tags = jsonItem.Tags.ToList(),
                    Author = jsonItem.Author,
                    CommentCount = jsonItem.CommentCount,

                    Content = () => Memoize(_cache, Path.Combine(dir, ITEM_CONTENT), key => File.ReadAllText(key)), 
                    Comments = () => Memoize(_cache, Path.Combine(dir, COMMENTS_JSON), 
                        key => JsonConvert.DeserializeObject<FSComment[]>(File.ReadAllText(key))
                            .Select(fsc => new Comment
                                {
                                    Id = fsc.Id,
                                    Content = fsc.Content,
                                    Date = fsc.Date,
                                    Author = fsc.Author, 
                                })),
                };

                tempPosts.Add(post);
                
                var compatFilePath = Path.Combine(dir, DASBLOG_COMPAT_JSON);
                if (File.Exists(compatFilePath))
                {
                    var compatItem = JsonConvert.DeserializeObject<FSDasBlogCompat>(File.ReadAllText(compatFilePath));
                    
                    _indexDasBlogEntryId[compatItem.EntryId] = post;
                    _indexDasBlogTitle[compatItem.Slug.ToLower()] = post;
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
            title = title.ToLower();
            return _indexDasBlogTitle.ContainsKey(title) ? _indexDasBlogTitle[title] : null;
        }
    }
}