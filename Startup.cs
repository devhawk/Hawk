using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
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
            
            app.UseMiddleware<DasBlogRedirector>();
            app.UseMiddleware<NotFoundMiddleware>();
            app.UseErrorPage(ErrorPageOptions.ShowAll);
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }
    }
}
