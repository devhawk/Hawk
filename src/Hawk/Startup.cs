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

            Action load = () => { throw new Exception("Post load action undefined"); };

            if (options.PostRepostitory == HawkOptions.PostRepositoryOptions.FileSystem)
            {
                var path = Configuration.Get("storage:FileSystemPath");
                logger.LogInformation("Loading posts from {path}", path);

                load = () => MemoryCachePostRepository.UpdateCache(cache, FileSystemRepo.EnumeratePosts(path));
            }

            if (options.PostRepostitory == HawkOptions.PostRepositoryOptions.Azure)
            {
                var account = Azure.CloudStorageAccount.Parse(options.AzureConnectionString);
                logger.LogInformation($"Loading posts from Azure ({account.Credentials.AccountName})");
                load = () =>
                {
                    var loadTask = AzureRepo.LoadFromAzureAsync(account);
                    loadTask.Wait();
                    MemoryCachePostRepository.UpdateCache(cache, loadTask.Result);
                };
            }

            load();
            cache.Set("Hawk.ReloadContent", load);

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
