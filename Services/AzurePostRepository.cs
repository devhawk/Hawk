using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Framework.Caching.Memory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Framework.Logging;

namespace Hawk
{
	class AzurePostRepository : IPostRepository
    {
        const string ITEM_CONTENT = "content.html";
        readonly CloudStorageAccount _storageAccount;

        Post[] _posts;
        Tuple<Category, int>[] _tags;
        Tuple<Category, int>[] _categories;
        readonly IMemoryCache _contentCache;
        readonly IMemoryCache _commentCache;
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
        
        static async Task<string> GetContent(CloudBlobContainer contentContainer, string key)
        {
            var htmlBlobRef = contentContainer.GetBlockBlobReference(key + "/rendered-content.html");
            
            string text;
            using (var memoryStream = new System.IO.MemoryStream())
            {
                await htmlBlobRef.DownloadToStreamAsync(memoryStream);
                text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            }
            
            return text;
        }
        
        static async Task<IEnumerable<Comment>> GetComments(CloudTable commentsTable, string key)
        {
            var comments = new List<Comment>();
            var query = new TableQuery<DynamicTableEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, key));
            
            var token = new TableContinuationToken();
            do
            {
                var segment = await commentsTable.ExecuteQuerySegmentedAsync(query, token);
                foreach (var dte in segment)
                {
                    comments.Add(new Comment
                    {
                        Content = dte.Properties["Content"].StringValue,
                        Date = dte.Properties["Date"].DateTimeOffsetValue.Value,
                        Author = new CommentAuthor
                        {
                            Name = dte.Properties["AuthorName"].StringValue,
                            Email = dte.Properties["AuthorEmail"].StringValue,
                            Url = dte.Properties["AuthorUrl"].StringValue,
                        },
                    });
                }

                token = segment.ContinuationToken;
            }
            while (token != null);
            
            return comments;
        }
        
        static IEnumerable<Category> ConvertCategories(string text)
        {
            return string.IsNullOrEmpty(text)
                ? Enumerable.Empty<Category>()
                : text.Split(',')
                    .Select(s => s.Split('|'))
                    .Select((string[] a) => new Category
                    {
                        Title = a[0],
                        Slug = a[1],
                    });
        }

        static PostAuthor ConvertAuthor(string text)
        {
            var a = text.Split('|');
            return new PostAuthor
            {
                Name = a[0],
                Slug = a[1],
                Email = a[2],
            };
        }
        
        public AzurePostRepository(CloudStorageAccount storageAccount)
        {
            _storageAccount = storageAccount;
            _contentCache = new MemoryCache(new MemoryCacheOptions());
            _commentCache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task InitializeAsync(ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger(nameof(FileSystemPostRepository));
            logger.LogInformation($"Loading data from from {_storageAccount.Credentials.AccountName}");

            var blobClient = _storageAccount.CreateCloudBlobClient();
            var tableClient = _storageAccount.CreateCloudTableClient();

            var contentContainer = blobClient.GetContainerReference("blog-content");
            var postsTable = tableClient.GetTableReference("blogPosts");
            var commentsTable = tableClient.GetTableReference("blogComments");

            var tempPosts = new List<Post>();

            var query = new TableQuery<DynamicTableEntity>();
            var token = new TableContinuationToken();
            do
            {
                var segment = await postsTable.ExecuteQuerySegmentedAsync(query, token);

                foreach (var azPost in segment)
                {
                    var post = new Post()
                    {
                        Slug = azPost.Properties["Slug"].StringValue,
                        Title = System.Net.WebUtility.HtmlDecode(azPost.Properties["Title"].StringValue),
                        Date = azPost.Properties["Date"].DateTimeOffsetValue.Value,
                        DateModified = azPost.Properties["Modified"].DateTimeOffsetValue.Value,
                        Categories = ConvertCategories(azPost.Properties["Categories"].StringValue).ToList(),
                        Tags = ConvertCategories(azPost.Properties["Tags"].StringValue).ToList(),
                        Author = ConvertAuthor(azPost.Properties["Author"].StringValue),
                        CommentCount = azPost.Properties["CommentCount"].Int32Value.Value,
                        Content = () => _contentCache.AsyncMemoize(azPost.PartitionKey, key => GetContent(contentContainer, key)), 
                        Comments = () => _commentCache.AsyncMemoize(azPost.PartitionKey, key => GetComments(commentsTable, key)), 
                    };
    
                    tempPosts.Add(post);
                                        
                    if (azPost.Properties.ContainsKey("DasBlogEntryId"))
                    {
                        _indexDasBlogEntryId[azPost.Properties["DasBlogEntryId"].GuidValue.Value] = post;
                    }
                    if (azPost.Properties.ContainsKey("DasBlogTitle"))
                    {
                        _indexDasBlogTitle[azPost.Properties["DasBlogTitle"].StringValue.ToLower()] = post;
                    }
                    if (azPost.Properties.ContainsKey("DasBlogUniqueTitle"))
                    {
                        _indexDasBlogTitle[azPost.Properties["DasBlogUniqueTitle"].StringValue.ToLower()] = post;
                    }
                }

                token = segment.ContinuationToken;
            }
            while (token != null);

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
    }
}