using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.StaticFiles;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using Newtonsoft.Json.Linq;
using Azure = Microsoft.WindowsAzure.Storage;

namespace Hawk
{
    public class Startup
    {
        public Startup(IHostingEnvironment env, IApplicationEnvironment appEnv)
        {
            // Setup configuration sources.
            var builder = new ConfigurationBuilder(appEnv.ApplicationBasePath)
                .AddJsonFile("config.json", true)
                .AddJsonFile($"config.{env.EnvironmentName}.json", true);
                
            builder.AddUserSecrets();
            if (env.IsDevelopment())
            {
                builder.AddApplicationInsightsSettings(true);
            }
                
            builder.AddEnvironmentVariables();
            
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; set; }

        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddCaching();
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddTransient<IPostRepository, MemoryCachePostRepository>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IMemoryCache cache)
        {
            loggerFactory.MinimumLevel = LogLevel.Information;
            loggerFactory.AddConsole();
            
            var logger = loggerFactory.CreateLogger(nameof(Startup));

            // TODO: drive data load from config. 
            #region temp data load

            // temp: syncronously load blog data from file system
            //logger.LogInformation("Loading posts from {path}", path);
            //var path = Configuration.Get("storage:FileSystemPath");
            //var posts = LoadPostsFromFileSystem(path);

            // temp: syncronously load blog data from Azure dev storage
            logger.LogInformation("Loading posts from Azure development storage");
            var posts = AzureRepo.LoadFromAzure(Azure.CloudStorageAccount.DevelopmentStorageAccount);
            MemoryCachePostRepository.UpdateCache(cache, posts);

            #endregion

            app.UseApplicationInsightsRequestTelemetry();
            
            // Use the error page only in development environment.
            if (env.IsDevelopment())
            {
                app.UseErrorPage();
            }
            else
            {
                // TODO: Add Error Handler
                //  app.UseErrorHandler("/Home/Error");
            }

            app.UseApplicationInsightsExceptionTelemetry();
            
            app.UseMiddleware<DasBlogRedirector>();
            app.UseMiddleware<NotFoundMiddleware>();
            app.UseStaticFiles(new StaticFileOptions() { ServeUnknownFileTypes = env.IsDevelopment() });
            app.UseMvcWithDefaultRoute();
        }

        static IEnumerable<Post> LoadPostsFromFileSystem(string path)
        {
            const string ITEM_JSON = "hawk-post.json";
            const string COMMENTS_JSON = "hawk-comments.json";
            const string ITEM_CONTENT = "rendered-content.html";

            var fsPosts = Directory
                .EnumerateDirectories(path)
                .Where(dir => File.Exists(Path.Combine(dir, ITEM_JSON)))
                .Select(dir => new
                {
                    Directory = dir,
                    Post = JObject.Parse(File.ReadAllText(Path.Combine(dir, ITEM_JSON))),
                });

            foreach (var fsPost in fsPosts)
            {
                yield return new Post()
                {
                    Slug = (string)fsPost.Post["slug"],
                    Title = System.Net.WebUtility.HtmlDecode((string)fsPost.Post["title"]),
                    Date = DateTimeOffset.Parse((string)fsPost.Post["date"]),
                    DateModified = DateTimeOffset.Parse((string)fsPost.Post["modified"]),
                    Categories = Category.FromCsvCatString((string)fsPost.Post["csv-category-slugs"]).ToList(),
                    Tags = Category.FromCsvCatString((string)fsPost.Post["csv-tag-slugs"]).ToList(),
                    Author = PostAuthor.FromString((string)fsPost.Post["author"]),
                    CommentCount = int.Parse((string)fsPost.Post["comment-count"]),

                    DasBlogEntryId = fsPost.Post["dasblog-entry-id"] != null ? Guid.Parse((string)fsPost.Post["dasblog-entry-id"]) : (Guid?)null,
                    DasBlogTitle = (string)fsPost.Post["dasblog-title"] ?? null,
                    DasBlogUniqueTitle = (string)fsPost.Post["dasblog-unique-title"] ?? null,

                    Content = () => Task.Run(() => File.ReadAllText(Path.Combine(fsPost.Directory, ITEM_CONTENT))),
                    Comments = () => Task.Run(() => JArray
                        .Parse(File.ReadAllText(Path.Combine(fsPost.Directory, COMMENTS_JSON)))
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
}
