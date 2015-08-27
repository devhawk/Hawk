using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Hawk.Models;

namespace Hawk.Services
{
    static class AzureRepo
    {
        static Task<string> GetContent(CloudBlobContainer contentContainer, string key)
        {
            var htmlBlobRef = contentContainer.GetBlockBlobReference(key + "/rendered-content.html");
            return htmlBlobRef.DownloadTextAsync();
        }

        static Comment ConvertComment(DynamicTableEntity dte)
        {
            return new Comment
            {
                Content = dte.Properties["Content"].StringValue,
                Date = dte.Properties["Date"].DateTimeOffsetValue.Value,
                Author = new CommentAuthor
                {
                    Name = dte.Properties["AuthorName"].StringValue,
                    Email = dte.Properties["AuthorEmail"].StringValue,
                    Url = dte.Properties["AuthorUrl"].StringValue,
                },
            };
        }

        static async Task<IEnumerable<T>> GetResults<T>(CloudTable table, TableQuery<T> query) where T : ITableEntity, new()
        {
            var resultsList = new List<List<T>>();

            var token = new TableContinuationToken();
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync(query, token);

                resultsList.Add(segment.Results);

                token = segment.ContinuationToken;
            }
            while (token != null);

            return resultsList.SelectMany(s => s);
        }

        static async Task<IEnumerable<Comment>> GetComments(CloudTable commentsTable, string key)
        {
            var comments = new List<Comment>();
            var query = new TableQuery<DynamicTableEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, key));

            var results = await GetResults(commentsTable, query);
            return results.Select(r => ConvertComment(r));
        }

        static Post ConvertPost(DynamicTableEntity dte, CloudBlobContainer contentContainer, CloudTable commentsTable)
        {
            return new Post()
            {
                Slug = dte.Properties["Slug"].StringValue,
                Title = System.Net.WebUtility.HtmlDecode(dte.Properties["Title"].StringValue),
                Date = dte.Properties["Date"].DateTimeOffsetValue.Value,
                DateModified = dte.Properties["Modified"].DateTimeOffsetValue.Value,
                Categories = Category.FromCsvCatString(dte.Properties["Categories"].StringValue).ToList(),
                Tags = Category.FromCsvCatString(dte.Properties["Tags"].StringValue).ToList(),
                Author = PostAuthor.FromString(dte.Properties["Author"].StringValue),
                CommentCount = dte.Properties["CommentCount"].Int32Value.Value,

                DasBlogEntryId = dte.Properties.ContainsKey("DasBlogEntryId") ? dte.Properties["DasBlogEntryId"].GuidValue.Value : (Guid?)null,
                DasBlogTitle = dte.Properties.ContainsKey("DasBlogTitle") ? dte.Properties["DasBlogTitle"].StringValue : null,
                DasBlogUniqueTitle = dte.Properties.ContainsKey("DasBlogUniqueTitle") ? dte.Properties["DasBlogUniqueTitle"].StringValue : null,

                Content = () => GetContent(contentContainer, dte.PartitionKey),
                Comments = () => GetComments(commentsTable, dte.PartitionKey),
            };
        }

        public static async Task<IEnumerable<Post>> LoadFromAzureAsync(CloudStorageAccount storageAccount)
        {
            var blobClient = storageAccount.CreateCloudBlobClient();
            var tableClient = storageAccount.CreateCloudTableClient();

            var contentContainer = blobClient.GetContainerReference("blog-content");
            var postsTable = tableClient.GetTableReference("blogPosts");
            var commentsTable = tableClient.GetTableReference("blogComments");

            var query = new TableQuery<DynamicTableEntity>();

            var results = await GetResults(postsTable, query);
            return results.Select(r => ConvertPost(r, contentContainer, commentsTable));
        }
    }
}