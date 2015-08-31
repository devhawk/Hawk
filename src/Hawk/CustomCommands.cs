using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Hawk.Models;
using Hawk.Services;
using Newtonsoft.Json.Linq;

namespace Hawk
{
    public class CustomCommands
    {
        IServiceProvider _provider;
        public CustomCommands(IServiceProvider provider)
        {
            _provider = provider;
        }

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

        public void ProcessCategoriesAndTags()
        {
            //TODO: read path from config
            var path = "E:\\dev\\DevHawk\\Content\\";
            var posts = FileSystemRepo.EnumeratePosts(path);
            var comparer = new FuncEqualityComparer<Category>((a, b) => a.Slug == b.Slug, a => a.Slug.GetHashCode());

            var cats = posts
                .SelectMany(p => p.Categories)
                .Distinct(comparer)
                .ToDictionary(c => c.Slug, c => Category.ToString(c));

            var tags = posts
                .SelectMany(p => p.Tags)
                .Distinct(comparer)
                .ToDictionary(c => c.Slug, c => Category.ToString(c));

            var jsonCat = new JObject();
            foreach (var cat in cats.OrderBy(kvp => kvp.Key))
            {
                jsonCat.Add(cat.Key, JValue.CreateString(cat.Value));
            }

            var jsonTag = new JObject();
            foreach (var tag in tags.OrderBy(kvp => kvp.Key))
            {
                jsonTag.Add(tag.Key, JValue.CreateString(tag.Value));
            }

            var json = new JObject();
            json.Add("categories", jsonCat);
            json.Add("tags", jsonTag);

            File.WriteAllText(
                Path.Combine(path, "CategoriesAndTags.json"), 
                json.ToString());
        }

        static async Task WritePostsToAzureAsync()
        {
            //TODO: get storage account info from config
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;

            var blobClient = storageAccount.CreateCloudBlobClient();
            var tableClient = storageAccount.CreateCloudTableClient();

            var contentContainer = blobClient.GetContainerReference("blog-content");
            await contentContainer.CreateIfNotExistsAsync();
            var postsTable = tableClient.GetTableReference("blogPosts");
            await postsTable.CreateIfNotExistsAsync();
            var commentsTable = tableClient.GetTableReference("blogComments");
            await commentsTable.CreateIfNotExistsAsync();

            //TODO: read path from config
            var path = "E:\\dev\\DevHawk\\Content\\";
            var posts = FileSystemRepo.EnumeratePostDirectories(path)
                .Reverse()
                .Select(dir => new
                {
                    Directory = dir,
                    Post = Post.FromDirectory(dir),
                });

            foreach (var post in posts)
            {
                Console.WriteLine(Path.GetFileName(post.Directory));

                var postDte = Post.ToDte(post.Post);
                var postResult = await postsTable.ExecuteAsync(TableOperation.InsertOrReplace(postDte));

                var commentsBatch = new TableBatchOperation();
                foreach (var comment in await post.Post.Comments())
                {
                    var commentDte = Comment.ToDte(comment, post.Post.UniqueKey);
                    commentsBatch.Add(TableOperation.InsertOrReplace(commentDte));
                }

                if (commentsBatch.Count > 0)
                {
                    var commentResult = await commentsTable.ExecuteBatchAsync(commentsBatch);
                }

                var contentPath = Path.Combine(post.Directory, FileSystemRepo.CONTENT_FILENAME);
                var contentBlob = contentContainer.GetBlockBlobReference($"{post.Post.UniqueKey}/{FileSystemRepo.CONTENT_FILENAME}");
                await contentBlob.UploadFromFileAsync(contentPath, FileMode.Open);

                var renderedContentPath = Path.Combine(post.Directory, FileSystemRepo.RENDERED_CONTENT_FILENAME);
                var renderedContentBlob = contentContainer.GetBlockBlobReference($"{post.Post.UniqueKey}/{FileSystemRepo.RENDERED_CONTENT_FILENAME}");
                await renderedContentBlob.UploadFromFileAsync(renderedContentPath, FileMode.Open);

                var imagePaths = Directory.EnumerateFiles(post.Directory)
                            .Where(p => FileSystemRepo.IMG_EXTENSIONS.Contains(Path.GetExtension(p).ToLowerInvariant()));
                foreach (var imagePath in imagePaths)
                {
                    var imageBlob = contentContainer.GetBlockBlobReference($"{post.Post.UniqueKey}/{Path.GetFileName(imagePath).ToLowerInvariant()}");
                    await imageBlob.UploadFromFileAsync(imagePath, FileMode.Open);
                }
            }
        }

        public void WritePostsToAzure()
        {
            // helper until I add async method support to CustomCommands
            WritePostsToAzureAsync().Wait();
        }
    }
}
