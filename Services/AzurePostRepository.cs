using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;

using Microsoft.Framework.Caching.Memory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace HawkProto2
{
	class AzurePostRepository : IPostRepository
    {
        const string ITEM_CONTENT = "content.html";
        
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
        
        static IObservable<T> ObserveTableQuery<T>(CloudTable table, TableQuery<T> query) where T : ITableEntity, new()
        {
            return System.Reactive.Linq.Observable.Create<T>(
                async obs =>
                {
                    var token = new TableContinuationToken();
                    do
                    {
                        var segment = await table.ExecuteQuerySegmentedAsync<T>(query, token);

                        foreach (var entry in segment)
                        {
                            obs.OnNext(entry);
                        }

                        token = segment.ContinuationToken;
                    }
                    while (token != null);
                });
        }

        static IEnumerable<T> EnumerateTableQuery<T>(CloudTable table, TableQuery<T> query) where T : ITableEntity, new()
        {
            return ObserveTableQuery(table, query).ToEnumerable();
        }

        static async Task<string> GetContent(CloudBlobContainer contentContainer, string key)
        {
            var htmlBlobRef = contentContainer.GetBlockBlobReference(key + "/content.html");
            
            string text;
            using (var memoryStream = new System.IO.MemoryStream())
            {
                await htmlBlobRef.DownloadToStreamAsync(memoryStream);
                text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            }
            
            return text;
        }
        
        static IEnumerable<Comment> GetComments(CloudTable commentsTable, string key)
        {
            var query = new TableQuery<DynamicTableEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, key));
            
            return EnumerateTableQuery(commentsTable, query)
                .Select(dte => new Comment
                    {
                        Id = dte.Properties["Id"].Int32Value.Value,
                        Content = dte.Properties["Content"].StringValue,
                        Date = dte.Properties["Date"].DateTimeOffsetValue.Value,
                        Author = new CommentAuthor
                        {
                            Name = dte.Properties["AuthorName"].StringValue,
                            Email = dte.Properties["AuthorEmail"].StringValue,
                            Url = dte.Properties["AuthorUrl"].StringValue,
                        },
                    })
                .ToArray();
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
        
        AzurePostRepository(IEnumerable<DynamicTableEntity> entities, CloudStorageAccount storageAccount)
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
            var tempPosts = new List<Post>();
        
            var blobClient = storageAccount.CreateCloudBlobClient();
            var contentContainer = blobClient.GetContainerReference("blog-content");
            var tableClient = storageAccount.CreateCloudTableClient();
            var commentsTable = tableClient.GetTableReference("blogComments");

            foreach (var entity in entities)
            {
                var post = new Post()
                {
                    Slug = entity.Properties["Slug"].StringValue,
                    Title = System.Net.WebUtility.HtmlDecode(entity.Properties["Title"].StringValue),
                    Date = entity.Properties["Date"].DateTimeOffsetValue.Value,
                    DateModified = entity.Properties["Modified"].DateTimeOffsetValue.Value,
                    Categories = ConvertCategories(entity.Properties["CsvCategorySlugs"].StringValue).ToList(),
                    Tags = ConvertCategories(entity.Properties["CsvTagSlugs"].StringValue).ToList(),
                    Author = ConvertAuthor(entity.Properties["Author"].StringValue),
                    CommentCount = entity.Properties["CommentCount"].Int32Value.Value,
                    Content = () => _cache.AsyncMemoize(entity.PartitionKey, key => GetContent(contentContainer, key)), 
                    //  Comments = () => _cache.Memoize(entity.PartitionKey + "-Comments", key => GetComments(commentsTable, key)),
                    Comments = () => GetComments(commentsTable, entity.PartitionKey),
                };

                tempPosts.Add(post);
                                    
                if (entity.Properties.ContainsKey("DasBlogEntryId"))
                {
                    _indexDasBlogEntryId[entity.Properties["DasBlogEntryId"].GuidValue.Value] = post;
                }
                if (entity.Properties.ContainsKey("DasBlogTitle"))
                {
                    _indexDasBlogTitle[entity.Properties["DasBlogTitle"].StringValue.ToLower()] = post;
                }
                if (entity.Properties.ContainsKey("DasBlogUniqueTitle"))
                {
                    _indexDasBlogTitle[entity.Properties["DasBlogUniqueTitle"].StringValue.ToLower()] = post;
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
        
        public static IPostRepository GetRepository(CloudStorageAccount storageAccount)
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            var entriesTable = tableClient.GetTableReference("blogEntries");
            var query = new TableQuery<DynamicTableEntity>();
            
            return new AzurePostRepository(EnumerateTableQuery(entriesTable, query), storageAccount);
        }
    }
}