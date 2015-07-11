using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

namespace HawkProto2
{
    public class Startup
    {
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddTransient<IPostRepository, HawkFileSystemPostRepository>();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.MinimumLevel = LogLevel.Information;
            loggerFactory.AddConsole();
            
            //  var logger = loggerFactory.CreateLogger("Test");
            //  app.Use (next => async context =>
            //  {
            //      logger.LogInformation(context.Request.Path);
            //      await next(context);
            //      logger.LogInformation(context.Response.StatusCode.ToString());
            //  });
            
            app.UseMiddleware<DasBlogRedirector>();
            app.UseMvcWithDefaultRoute();
        }
    }
}
