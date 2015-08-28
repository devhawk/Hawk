using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;
using System.IO;
using Hawk.Services;

namespace Hawk.Models
{
    public class Post
    {
        public string Slug { get; set; }
        public string Title { get; set; }
        public DateTimeOffset Date { get; set; }
        public DateTimeOffset DateModified { get; set; }

        public IList<Category> Categories { get; set; }
        public IList<Category> Tags { get; set; }
        public PostAuthor Author { get; set; }
        
        public int CommentCount { get; set; }

        public Guid? DasBlogEntryId { get; set; }
        public string DasBlogTitle { get; set; }
        public string DasBlogUniqueTitle { get; set; }

        public Func<Task<string>> Content { get; set; }
        public Func<Task<IEnumerable<Comment>>> Comments { get; set; }

        public string UniqueKey
        {
            get
            {
                return Date.ToString("yyyyMMdd-HHmm-") + Slug;
            }
        }

        static async Task<IEnumerable<Comment>> GetComments(CloudTable commentsTable, string partitionKey)
        {
            var comments = new List<Comment>();
            var query = new TableQuery<DynamicTableEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

            var results = await Services.AzureRepo.GetResults(commentsTable, query);
            return results.Select(r => Comment.FromDte(r));
        }

        public static Post FromDte(DynamicTableEntity dte, CloudBlobContainer contentContainer, CloudTable commentsTable)
        {
            return new Post()
            {
                Slug = dte.Properties["Slug"].StringValue,
                Title = System.Net.WebUtility.HtmlDecode(dte.Properties["Title"].StringValue),
                Date = dte.Properties["Date"].DateTimeOffsetValue.Value,
                DateModified = dte.Properties["Modified"].DateTimeOffsetValue.Value,
                Categories = Category.FromString(dte.Properties["Categories"].StringValue).ToList(),
                Tags = Category.FromString(dte.Properties["Tags"].StringValue).ToList(),
                Author = PostAuthor.FromString(dte.Properties["Author"].StringValue),
                CommentCount = dte.Properties["CommentCount"].Int32Value.Value,

                DasBlogEntryId = dte.Properties.ContainsKey("DasBlogEntryId") ? dte.Properties["DasBlogEntryId"].GuidValue.Value : (Guid?)null,
                DasBlogTitle = dte.Properties.ContainsKey("DasBlogTitle") ? dte.Properties["DasBlogTitle"].StringValue : null,
                DasBlogUniqueTitle = dte.Properties.ContainsKey("DasBlogUniqueTitle") ? dte.Properties["DasBlogUniqueTitle"].StringValue : null,

                Content = () => contentContainer.GetBlockBlobReference(dte.PartitionKey + "/rendered-content.html").DownloadTextAsync(),
                Comments = () => GetComments(commentsTable, dte.PartitionKey),
            };
        }

        public static Post FromDirectory(string directory)
        {
            //.Select(dir => Tuple.Create(dir, JsonToPost(dir, 


            var jsonPost = JObject.Parse(File.ReadAllText(Path.Combine(directory, FileSystemRepo.ITEM_JSON)));

            return new Post()
            {
                Slug = (string)jsonPost["slug"],
                Title = System.Net.WebUtility.HtmlDecode((string)jsonPost["title"]),
                Date = DateTimeOffset.Parse((string)jsonPost["date"]),
                DateModified = DateTimeOffset.Parse((string)jsonPost["modified"]),
                Categories = Category.FromString((string)jsonPost["csv-category-slugs"]).ToList(),
                Tags = Category.FromString((string)jsonPost["csv-tag-slugs"]).ToList(),
                Author = PostAuthor.FromString((string)jsonPost["author"]),
                CommentCount = int.Parse((string)jsonPost["comment-count"]),

                DasBlogEntryId = jsonPost["dasblog-entry-id"] != null ? Guid.Parse((string)jsonPost["dasblog-entry-id"]) : (Guid?)null,
                DasBlogTitle = (string)jsonPost["dasblog-title"] ?? null,
                DasBlogUniqueTitle = (string)jsonPost["dasblog-unique-title"] ?? null,

                Content = () => Task.Run(() => File.ReadAllText(Path.Combine(directory, FileSystemRepo.ITEM_CONTENT))),
                Comments = () => Task.Run(() => JArray
                    .Parse(File.ReadAllText(Path.Combine(directory, FileSystemRepo.COMMENTS_JSON)))
                    .Select(fsc => new Comment
                    {
                        Content = (string)fsc["content"],
                        Date = DateTimeOffset.Parse((string)fsc["date"]),
                        Author = new CommentAuthor
                        {
                            Name = (string)fsc["author-name"],
                            Email = (string)fsc["author-email"],
                            Url = (string)fsc["author-url"],
                        }
                    })),
            };
        }



    }
}