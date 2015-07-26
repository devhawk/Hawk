using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.StaticFiles;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using Azure = Microsoft.WindowsAzure.Storage;

namespace HawkProto2
{
    public class Startup
    {
        public Startup(IHostingEnvironment env, IApplicationEnvironment appEnv)
        {
            // Setup configuration sources.
            var builder = new ConfigurationBuilder(appEnv.ApplicationBasePath)
                .AddUserSecrets()
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; set; }

        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            var creds = new Azure.Auth.StorageCredentials("hawkblogstorage", Configuration.Get("storageKey"));
            var account = new Azure.CloudStorageAccount(creds, false);
            services.AddInstance<IPostRepository>(AzurePostRepository.GetRepository(account));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.MinimumLevel = LogLevel.Information;
            loggerFactory.AddConsole();
            
            // Use the error page only in development environment.
            if (env.IsDevelopment())
            {
                app.UseErrorPage(ErrorPageOptions.ShowAll);
            }
            else
            {
                // TODO: Add Error Handler
                //  app.UseErrorHandler("/Home/Error");
            }

            app.UseMiddleware<DasBlogRedirector>();
            app.UseMiddleware<NotFoundMiddleware>();
            app.UseStaticFiles(new StaticFileOptions() { ServeUnknownFileTypes = env.IsDevelopment() });
            app.UseMvcWithDefaultRoute();
        }
    }
}
