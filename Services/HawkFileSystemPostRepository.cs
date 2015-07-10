using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Framework.Logging;

namespace HawkProto2
{
	public class FSDasBlogCompat
	{
        [JsonProperty("entry-id")]
	    public string EntryId { get; set; }
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
        const string ITEM_CONTENT = "rendered-content.html";
        
        static Post[] _posts = null;
        static Tuple<Category, int>[] _tags = null;
        static Tuple<Category, int>[] _categories = null;

        static HawkFileSystemPostRepository()
        {
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

                    Content = new Lazy<string>(() => File.ReadAllText(Path.Combine(dir, ITEM_CONTENT))),
                    Comments = new Lazy<IEnumerable<Comment>>(() => 
                        {
                            var text = File.ReadAllText(Path.Combine(dir, COMMENTS_JSON));
                            return JsonConvert.DeserializeObject<FSComment[]>(text)
                                .Select(fsc => new Comment
                                    {
                                        Id = fsc.Id,
                                        Content = fsc.Content,
                                        Date = fsc.Date,
                                        Author = fsc.Author, 
                                    });
                        }),
                };

                tempPosts.Add(post);
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
    }
}