using Hawk.Models;
using Hawk.Services;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hawk
{
    public static class CustomCommands
    {
        class FuncEqualityComparer<T> : IEqualityComparer<T>
        {
            readonly Func<T, T, bool> _comparer;
            readonly Func<T, int> _hash;

            public FuncEqualityComparer(Func<T, T, bool> comparer)
                : this(comparer, t => 0) // NB Cannot assume anything about how e.g., t.GetHashCode() interacts with the comparer's behavior
            {
            }

            public FuncEqualityComparer(Func<T, T, bool> comparer, Func<T, int> hash)
            {
                _comparer = comparer;
                _hash = hash;
            }

            public bool Equals(T x, T y)
            {
                return _comparer(x, y);
            }

            public int GetHashCode(T obj)
            {
                return _hash(obj);
            }
        }

        public static void ProcessCategoriesAndTags()
        {
            var comparer = new FuncEqualityComparer<Category>((a, b) => a.Slug == b.Slug, a => a.Slug.GetHashCode());

            var path = "E:\\dev\\DevHawk\\Content\\";
            var posts = FileSystemRepo.EnumeratePosts(path);

            var cats = posts
                .SelectMany(t => t.Categories)
                .Distinct(comparer)
                .ToDictionary(c => c.Slug, c => c.Title);

            var catsJson = JsonConvert.SerializeObject(cats);

            var tags = posts
                .SelectMany(t => t.Tags)
                .Distinct(comparer)
                .ToDictionary(c => c.Slug, c => c.Title);

            var tagsJson = JsonConvert.SerializeObject(tags);
        }

        //public static async Task WritePostsToAzureAsync()
        //{
        //    var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;

        //    var blobClient = storageAccount.CreateCloudBlobClient();
        //    var tableClient = storageAccount.CreateCloudTableClient();

        //    var contentContainer = blobClient.GetContainerReference("blog-content");
        //    await contentContainer.CreateIfNotExistsAsync();
        //    var postsTable = tableClient.GetTableReference("blogPosts");
        //    await postsTable.CreateIfNotExistsAsync();
        //    var commentsTable = tableClient.GetTableReference("blogComments");
        //    await commentsTable.CreateIfNotExistsAsync();


        //}
    }
}
