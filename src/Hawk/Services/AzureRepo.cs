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
        // eventually should move this class into some helper class
        public static async Task<IEnumerable<T>> GetResults<T>(CloudTable table, TableQuery<T> query) where T : ITableEntity, new()
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

        public static async Task<IEnumerable<Post>> LoadFromAzureAsync(CloudStorageAccount storageAccount)
        {
            var blobClient = storageAccount.CreateCloudBlobClient();
            var tableClient = storageAccount.CreateCloudTableClient();

            var contentContainer = blobClient.GetContainerReference("blog-content");
            var postsTable = tableClient.GetTableReference("blogPosts");
            var commentsTable = tableClient.GetTableReference("blogComments");

            var query = new TableQuery<DynamicTableEntity>();

            var results = await GetResults(postsTable, query);
            return results.Select(r => Post.FromDte(r, contentContainer, commentsTable));
        }
    }
}