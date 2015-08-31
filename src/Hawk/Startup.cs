using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.StaticFiles;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using Hawk.Middleware;
using Hawk.Services;
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

            //builder.AddUserSecrets();
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
            //var path = Configuration.Get("storage:FileSystemPath");
            //logger.LogInformation("Loading posts from {path}", path);
            //Action load = () => MemoryCachePostRepository.UpdateCache(cache, FileSystemRepo.EnumeratePosts(path));

            // temp: syncronously load blog data from Azure dev storage
            var connString = Configuration.Get("storage:AzureConnectionString");
            var account = Azure.CloudStorageAccount.Parse(connString);
            logger.LogInformation($"Loading posts from Azure ({account.Credentials.AccountName})");
            Action load = () =>
            {
                var loadTask = AzureRepo.LoadFromAzureAsync(account);
                loadTask.Wait();
                MemoryCachePostRepository.UpdateCache(cache, loadTask.Result);
            };

            load();
            cache.Set("Hawk.ReloadContent", load);

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
    }
}