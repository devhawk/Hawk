using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.StaticFiles;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using Azure = Microsoft.WindowsAzure.Storage;
using System.Threading.Tasks;
using Microsoft.Framework.Caching.Memory;
using System.Linq;

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

            // this code feels a little smelly. 
            //   * Should I be injecting singletons by type instead of instances? 
            //      * If so, how do I access the configuration object
            //   * Should I be using transient objects instead of single instances?
            //      * The metadata for all my posts is about 2/3 a MB encoded as JSON. So keeping it all in memory seems like the right choice.
            //      * should I be using the memory cache object? Currently, all the metadata is stored in fields inside the single repo object
            //      * Still need to figure out how I'm going to detect changes. I'm thinking of adding a webhook of some sort that I will 
            //        integrate into my authoring tools (once I build those)

            /*
            var postRepo = Configuration.Get("PostRepostitory");

            // default to using Azure
            if (string.IsNullOrEmpty(postRepo) || string.Equals(postRepo, "Azure", StringComparison.OrdinalIgnoreCase))
            {
                var accountName = Configuration.Get("storage:AccountName");
                var accountKey  = Configuration.Get("storage:AccountKey"); 
                
                Azure.CloudStorageAccount account;
                if (accountName == null || accountKey == null)
                {
                    // if both account name and key aren't specified, use the development account
                    // TODO : only support DevStorage when env isDevelopment
                    account = Azure.CloudStorageAccount.DevelopmentStorageAccount;
                }
                else
                {
                    var creds = new Azure.Auth.StorageCredentials(accountName, accountKey);
                    account = new Azure.CloudStorageAccount(creds, false);
                }

                services.AddInstance<IPostRepository>(new AzurePostRepository(account));
            } 
            // but also support using the file system
            else if (string.Equals(postRepo, "FileSystem", StringComparison.OrdinalIgnoreCase))
            {
                var path = Configuration.Get("storage:FileSystemPath");
                if (path == null)
                {
                    throw new Exception("FileSystem Storage path not property configured");
                }

                services.AddInstance<IPostRepository>(new FileSystemPostRepository(path));
            }  
            else {
                throw new Exception($"Invalid PostRepository specified ({postRepo})");
            }
            */
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IMemoryCache cache)
        {
            loggerFactory.MinimumLevel = LogLevel.Information;
            loggerFactory.AddConsole();
            
            var logger = loggerFactory.CreateLogger(nameof(Startup));

            // temporarily insert hard coded post
            Post p = new Post
            {
                Author = PostAuthor.ConvertPostAuthor("DevHawk|devhawk|harry@devhawk.net"),
                Categories = Category.ConvertCsvCatString("Sports|sports").ToList(),
                CommentCount = 0,
                Comments = async () => await Task.FromResult(Enumerable.Empty<Comment>()),
                Content = async () => await Task.FromResult("this is a test"),
                Date = DateTimeOffset.Parse("2014-04-26T13:13:29-07:00"),
                DateModified = DateTimeOffset.Parse("2014-04-26T13:13:29-07:00"),
                Slug = "yet-more-change-for-the-capitals",
                Tags = Category.ConvertCsvCatString("Hockey|hockey,Washington Capitals|washington-capitals").ToList(),
                Title = "Yet More Change for the Capitals",
            };

            MemoryCachePostRepository.UpdateCache(cache, new Post[] { p });
            
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
    }
}
