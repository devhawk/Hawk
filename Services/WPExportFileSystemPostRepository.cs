using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace HawkProto2
{
    public class WPCategory
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("description ")]
        public string Description { get; set; }
        [JsonProperty("parent")]
        public int Parent { get; set; }
        [JsonProperty("post_count")]
        public int PostCount { get; set; }
    }
    
    public class WPTag
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("description ")]
        public string Description { get; set; }
        [JsonProperty("post_count")]
        public int PostCount { get; set; }
    }
    
    public class WPAuthor
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("first_name")]
        public string FirstName { get; set; }
        [JsonProperty("last_name")]
        public string LastName { get; set; }
        [JsonProperty("nickname")]
        public string Nickname { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class WPComment
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("author")]
        public WPAuthor Author { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("parent")]
        public int Parent { get; set; }
    }
    
    public class WPCustomFields
    {
        [JsonProperty("dasblog_entryid")]
        public List<string> DasBlogEntryId { get; set; }
        [JsonProperty("dasblog_compressedtitle")]
        public List<string> DasBlogCompressedTitle { get; set; }
        [JsonProperty("dasblog_compressedtitleunique")]
        public List<string> DasBlogCompressedTitleUnique { get; set; }
        [JsonProperty("enclosure")]
        public List<string> Enclosure { get; set; }
        [JsonProperty("layout_key")]
        public List<string> LayoutKey { get; set; }
        [JsonProperty("post_slider_check_key")]
        public List<string> PostSliderCheckKey { get; set; }
    }
    
    public class WPImage
    {
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("width")]
        public int Width { get; set; }
        [JsonProperty("height")]
        public int Height { get; set; }
    }

    public class WPImages
    {
        [JsonProperty("full")]
        public WPImage Full { get; set; }
        [JsonProperty("thumbnail")]
        public WPImage Thumbnail { get; set; }
        [JsonProperty("medium")]
        public WPImage Medium { get; set; }
        [JsonProperty("large")]
        public WPImage Large { get; set; }
        [JsonProperty("tc-thumb")]
        public WPImage TcThumb { get; set; }
        [JsonProperty("slider-full")]
        public WPImage SliderFull { get; set; }
        [JsonProperty("slider")]
        public WPImage Slider { get; set; }
    }
    
    public class WPAttachment
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("description ")]
        public string Description { get; set; }
        [JsonProperty("caption")]
        public string Caption { get; set; }
        [JsonProperty("parent")]
        public int Parent { get; set; }
        [JsonProperty("mime_type")]
        public string MimeType { get; set; }
        [JsonProperty("images")]
        public WPImages Images { get; set; }
    }
    
    public class WPPost
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("title_plain")]
        public string TitlePlain { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("excerpt")]
        public string Excerpt { get; set; }
        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }
        [JsonProperty("modified")]
        public DateTimeOffset Modified { get; set; }
        [JsonProperty("categories")]
        public List<WPCategory> Categories { get; set; }
        [JsonProperty("tags")]
        public List<WPTag> Tags { get; set; }
        [JsonProperty("author")]
        public Author Author { get; set; }
        [JsonProperty("comments")]
        public List<WPComment> Comments { get; set; }
        [JsonProperty("attachements")]
        public List<WPAttachment> Attachments { get; set; }
        [JsonProperty("comment_count")]
        public int CommentCount { get; set; }
        [JsonProperty("comment_status")]
        public string CommentStatus { get; set; }
        [JsonProperty("custom_fields")]
        public WPCustomFields CustomFields { get; set; }
    }
    
    class WPJsonItem
    {
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("post")]
        public WPPost Post { get; set; }
        [JsonProperty("previous_url")]
        public string PreviousUrl { get; set; }
        [JsonProperty("next_url")]
        public string NextUrl { get; set; }
    }

    class WPExportFileSystemPostRepository : IPostRepository
    {
        const string PATH = @"E:\dev\DevHawk\Content\Posts";
        const string ITEM_JSON = "json-item.json";
        const string ITEM_CONTENT = "rendered-content.html";
        
        static Post[] _posts = null;
        static Tuple<Category, int>[] _tags = null;
        static Tuple<Category, int>[] _categories = null;

        static WPExportFileSystemPostRepository()
        {
            var tempPosts = new List<Post>();

            foreach (var dir in Directory.EnumerateDirectories(PATH))
            {
                var jsonItemText = File.ReadAllText(Path.Combine(dir, ITEM_JSON));
                var jsonItem = JsonConvert.DeserializeObject<WPJsonItem>(jsonItemText);
                
                var post = new Post()
                {
                    Slug = jsonItem.Post.Slug,
                    Title = System.Net.WebUtility.HtmlDecode(jsonItem.Post.Title),
                    Date = jsonItem.Post.Date,
                    DateModified = jsonItem.Post.Modified,
                    Categories = jsonItem.Post.Categories.Select(c => new Category { Title = c.Title, Slug = c.Slug }).ToList(),
                    Tags = jsonItem.Post.Tags.Select(c => new Category { Title = c.Title, Slug = c.Slug }).ToList(),
                    Author = new PostAuthor { Name = jsonItem.Post.Author.Name, Slug = jsonItem.Post.Author.Slug },       
                    CommentCount = jsonItem.Post.Comments.Count,
                    Content = new Lazy<string>(() => File.ReadAllText(Path.Combine(dir, ITEM_CONTENT))),
                    Comments = new Lazy<IEnumerable<Comment>>(() => jsonItem.Post.Comments.Select(wpc => new Comment
                        {
                            Id = wpc.Id,
                            Content = wpc.Content,
                            Date = wpc.Date,
                            Author = new CommentAuthor 
                            {
                                Name = wpc.Name,
                                Url = wpc.Url,
                            },
                        })),
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