using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.StaticFiles;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.Runtime;
using Hawk.Middleware;
using Hawk.Services;
using Azure = Microsoft.WindowsAzure.Storage;

namespace Hawk
{
    public class HawkOptions
    {
        public enum PostRepositoryOptions
        {
            Azure,
            FileSystem,
        }

        public PostRepositoryOptions PostRepostitory { get; set; }
        public string FileSystemPath { get; set; }
        public string AzureConnectionString { get; set; }
        public string RefreshKey { get; set; }
    }

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
                // put ApplicationInsights in developer mode
                builder.AddApplicationInsightsSettings(true);
            }

            builder.AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; set; }

        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<HawkOptions>(Configuration);

            services.AddMvc();
            services.AddCaching();
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddTransient<IPostRepository, MemoryCachePostRepository>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IMemoryCache cache, IOptions<HawkOptions> optionsAccessor)
        {
            loggerFactory.MinimumLevel = LogLevel.Information;
            loggerFactory.AddConsole();

            var logger = loggerFactory.CreateLogger(nameof(Startup));

            var options = optionsAccessor.Options;

            Action load = GetContentLoader(options, env, cache, logger);
            load();
            cache.Set("Hawk.ReloadContent", load);

            app.UseApplicationInsightsRequestTelemetry();

            if (env.IsDevelopment())
            {
                // Only use the detailed error page in development environment.
                app.UseErrorPage();
            }
            else
            {
                app.UseErrorHandler("/Home/Error");
            }

            app.UseMiddleware<DasBlogRedirector>();
            
            app.UseApplicationInsightsExceptionTelemetry();

            app.UseStaticFiles(new StaticFileOptions() { ServeUnknownFileTypes = env.IsDevelopment() });
            app.UseMvcWithDefaultRoute();
        }

        Action GetContentLoader(HawkOptions options, IHostingEnvironment env, IMemoryCache cache, ILogger logger)
        {
            if (options.PostRepostitory == HawkOptions.PostRepositoryOptions.FileSystem)
            {
                if (env.IsDevelopment() == false)
                {
                    throw new Exception("FileSystem repository can only be used in development environment.");
                }

                var path = options.FileSystemPath;
                logger.LogInformation("Loading posts from {path}", path);

                return () => MemoryCachePostRepository.UpdateCache(cache, FileSystemRepo.EnumeratePosts(path));
            }

            if (options.PostRepostitory == HawkOptions.PostRepositoryOptions.Azure)
            {
                var account = Azure.CloudStorageAccount.Parse(options.AzureConnectionString);

                if (env.IsDevelopment() == false && account.Credentials.AccountName == "devstoreaccount1")
                {
                    throw new Exception("Azure DevelopmentStorageAccount can only be used in development environment.");
                }

                logger.LogInformation($"Loading posts from Azure ({account.Credentials.AccountName})");
                return () =>
                {
                    // TODO: investiage if there is a better way to handle async operations during setup
                    var loadTask = AzureRepo.LoadFromAzureAsync(account);
                    loadTask.Wait();
                    MemoryCachePostRepository.UpdateCache(cache, loadTask.Result);
                };
            }

            throw new Exception("Invalid PostRepostitory configuration setting");
        }
    }
}
