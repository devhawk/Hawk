using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Azure = Microsoft.WindowsAzure.Storage;

namespace HawkProto2
{
    // TODO: Redirect /yyyy/MM/dd/slug and /slug entries to /blog/yyyy/MM/dd/slug
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
                    // if the request path looks like /yyyy/mm/dd/slug or /slug, redirect to the blog controller
        			var match = Regex.Match(context.Request.Path.Value ?? string.Empty, @"^/(\d\d\d\d/\d\d?/\d\d?/)?[_a-z0-0/-]*$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        logger.LogInformation("{Path}{QueryString} looks like a WP era URL. Redirecting to /blog{Path}", context.Request.Path, context.Request.QueryString, context.Request.Path);
                        context.Response.Redirect("/blog" + context.Request.Path, true);
                    }
                    else
                    {
                        logger.LogWarning("{Path}{QueryString} Not Found", context.Request.Path, context.Request.QueryString);
                    }
                }
            });
            
            app.UseMiddleware<DasBlogRedirector>();
            app.UseMvcWithDefaultRoute();
        }
    }
}
