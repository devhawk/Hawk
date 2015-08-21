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
            services.AddApplicationInsightsTelemetry(Configuration);
            
            var postRepo = Configuration.Get("PostRepostitory");
            
            // default to using Azure
            if (string.IsNullOrEmpty(postRepo) || string.Equals(postRepo, "Azure", StringComparison.OrdinalIgnoreCase))
            {
                var accountName = Configuration.Get("storage:AccountName");
                var accountKey  = Configuration.Get("storage:AccountKey"); 
                
                Azure.CloudStorageAccount account;
                if (accountName != null && accountKey != null)
                {
                    var creds = new Azure.Auth.StorageCredentials(accountName, accountKey);
                    account = new Azure.CloudStorageAccount(creds, false);
                }
                else
                {
                    account = Azure.CloudStorageAccount.DevelopmentStorageAccount;
                }
                
                services.AddInstance<IPostRepository>(AzurePostRepository.GetRepository(account));
            } 
            // but also support using the file system
            else if (string.Equals(postRepo, "FileSystem", StringComparison.OrdinalIgnoreCase))
            {
                var path = Configuration.Get("storage:FileSystemPath");
                if (path == null)
                {
                    throw new Exception("FileSystem Storage path not property configured");
                }

                services.AddInstance<IPostRepository>(FileSystemPostRepository.GetRepository(path));
            }  
            else {
                throw new Exception($"Invalid PostRepository specified ({postRepo})");
            }
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.MinimumLevel = LogLevel.Information;
            loggerFactory.AddConsole();
            
            var logger = loggerFactory.CreateLogger(nameof(Startup));
            
            string postRepo;
            if (Configuration.TryGet("PostRepostitory", out postRepo))
            {
                logger.LogInformation($"Using {postRepo} Post Repository.");    
            }
            else 
            {
                logger.LogInformation($"Using Azure Post Repository by default.");    
            }
            
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
