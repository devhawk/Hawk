using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;
using Hawk.Services;

namespace Hawk.Models
{
    public class Post
    {
        public string Slug { get; set; }
        public string Title { get; set; }
        public DateTimeOffset Date { get; set; }
        public DateTimeOffset DateModified { get; set; }

        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<Category> Tags { get; set; }
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
                return Date.ToString("yyyyMMdd-") + Slug;
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
                Title = WebUtility.HtmlDecode(dte.Properties["Title"].StringValue),
                Date = dte.Properties["Date"].DateTimeOffsetValue.Value,
                DateModified = dte.Properties["DateModified"].DateTimeOffsetValue.Value,
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

        public static DynamicTableEntity ToDte(Post post)
        {
            var dte = new DynamicTableEntity(post.UniqueKey, string.Empty);

            dte.Properties.Add("Slug", EntityProperty.GeneratePropertyForString(post.Slug));
            dte.Properties.Add("Title", EntityProperty.GeneratePropertyForString(WebUtility.HtmlEncode(post.Title)));
            dte.Properties.Add("Date", EntityProperty.GeneratePropertyForDateTimeOffset(post.Date));
            dte.Properties.Add("DateModified", EntityProperty.GeneratePropertyForDateTimeOffset(post.DateModified));
            dte.Properties.Add("Categories", EntityProperty.GeneratePropertyForString(Category.ToString(post.Categories)));
            dte.Properties.Add("Tags", EntityProperty.GeneratePropertyForString(Category.ToString(post.Tags)));
            dte.Properties.Add("Author", EntityProperty.GeneratePropertyForString(PostAuthor.ToString(post.Author)));
            dte.Properties.Add("CommentCount", EntityProperty.GeneratePropertyForInt(post.CommentCount));

            if (post.DasBlogEntryId.HasValue)
            {
                dte.Properties.Add("DasBlogEntryId", EntityProperty.GeneratePropertyForGuid(post.DasBlogEntryId.Value));
            }

            if (!string.IsNullOrEmpty(post.DasBlogTitle))
            {
                dte.Properties.Add("DasBlogTitle", EntityProperty.GeneratePropertyForString(post.DasBlogTitle));
            }

            if (!string.IsNullOrEmpty(post.DasBlogUniqueTitle))
            {
                dte.Properties.Add("DasBlogUniqueTitle", EntityProperty.GeneratePropertyForString(post.DasBlogUniqueTitle));
            }

            return dte;
        }

        public static Post FromDirectory(string directory)
        {
            var jsonPostPath = Path.Combine(directory, FileSystemRepo.ITEM_JSON);
            var jsonPost = JObject.Parse(File.ReadAllText(jsonPostPath));

            var jsonCommentsPath = Path.Combine(directory, FileSystemRepo.COMMENTS_JSON);

            return new Post()
            {
                Slug = (string)jsonPost["slug"],
                Title = System.Net.WebUtility.HtmlDecode((string)jsonPost["title"]),
                Date = DateTimeOffset.Parse((string)jsonPost["date"]),
                DateModified = DateTimeOffset.Parse((string)jsonPost["modified"]),
                Categories = Category.FromString((string)jsonPost["csv-category-slugs"]),
                Tags = Category.FromString((string)jsonPost["csv-tag-slugs"]),
                Author = PostAuthor.FromString((string)jsonPost["author"]),
                CommentCount = int.Parse((string)jsonPost["comment-count"]),

                DasBlogEntryId = jsonPost["dasblog-entry-id"] != null ? Guid.Parse((string)jsonPost["dasblog-entry-id"]) : (Guid?)null,
                DasBlogTitle = (string)jsonPost["dasblog-title"] ?? null,
                DasBlogUniqueTitle = (string)jsonPost["dasblog-unique-title"] ?? null,

                Content = () => Task.Run(() => File.ReadAllText(Path.Combine(directory, FileSystemRepo.RENDERED_CONTENT_FILENAME))),
                Comments = () => Task.Run(() => !File.Exists(jsonCommentsPath) ? Enumerable.Empty<Comment>() : JArray
                    .Parse(File.ReadAllText(jsonCommentsPath))
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