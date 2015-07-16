using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Azure = Microsoft.WindowsAzure.Storage;

namespace HawkProto2
{
    public class Startup
    {
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            //  services.AddInstance<IPostRepository>(HawkFileSystemPostRepository.GetRepository());
            services.AddInstance<IPostRepository>(AzurePostRepository.GetRepository(Azure.CloudStorageAccount.DevelopmentStorageAccount));
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.MinimumLevel = LogLevel.Information;
            loggerFactory.AddConsole();
            
            var logger = loggerFactory.CreateLogger("404 Tracker");
            app.Use (next => async context =>
            {
                await next(context);
                
                if (context.Response.StatusCode == 404)
                {
                    logger.LogInformation("{Path}{QueryString} Not Found", context.Request.Path, context.Request.QueryString);
                }
            });
            
            app.UseMiddleware<DasBlogRedirector>();
            app.UseMvcWithDefaultRoute();
        }
    }
}
