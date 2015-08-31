using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.Runtime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;
using Hawk.Models;
using Hawk.Services;

namespace Hawk
{
    public class CustomCommands
    {
        readonly IApplicationEnvironment _appEnv;
        readonly IServiceProvider _provider;

        public IConfiguration Configuration { get; set; }

        public CustomCommands(IServiceProvider provider, IApplicationEnvironment appEnv)
        {
            _appEnv = appEnv;
            _provider = provider;

            var builder = new ConfigurationBuilder(appEnv.ApplicationBasePath)
                .AddJsonFile("config.json", true)
                .AddJsonFile($"config.development.json", true)
                .AddUserSecrets()
                .AddEnvironmentVariables();

            Configuration = builder.Build();
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
            var path = Configuration.Get("FileSystemPath");
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

        async Task WritePostsToAzureAsync()
        {
            var path = Configuration.Get("FileSystemPath");
            var storageAccount = CloudStorageAccount.Parse(Configuration.Get("AzureConnectionString"));
            Console.WriteLine($"Uploading posts from {path} to {storageAccount.Credentials.AccountName}");

            var blobClient = storageAccount.CreateCloudBlobClient();
            var tableClient = storageAccount.CreateCloudTableClient();

            var contentContainer = blobClient.GetContainerReference("blog-content");
            await contentContainer.CreateIfNotExistsAsync();
            var postsTable = tableClient.GetTableReference("blogPosts");
            await postsTable.CreateIfNotExistsAsync();
            var commentsTable = tableClient.GetTableReference("blogComments");
            await commentsTable.CreateIfNotExistsAsync();

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

        public void FixPaths()
        {
            var path = Configuration.Get("FileSystemPath");
            var posts = FileSystemRepo.EnumeratePostDirectories(path)
                .Reverse()
                .Select(dir => new
                {
                    Directory = dir,
                    Post = Post.FromDirectory(dir),
                })
                .ToArray();

            foreach (var post in posts)
            {
                var oldDirName = Path.GetFileName(post.Directory);
                var newDirName = $"{post.Post.Date.ToString("yyyyMMdd")}-{post.Post.Slug}";
                if (oldDirName != newDirName)
                {
                    Console.WriteLine($"fixing {oldDirName}");
                    Directory.Move(Path.Combine(path, oldDirName), Path.Combine(path, newDirName));
                }
            }
        }
    }
}
