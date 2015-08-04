using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Microsoft.ApplicationInsights;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

namespace Hawk
{
	public class NotFoundMiddleware
	{
	    RequestDelegate _next;
	
	    public NotFoundMiddleware(RequestDelegate next)
	    {
	        _next = next;
	    }
		
	    public async Task Invoke(HttpContext context)
	    {
			var loggerFactory = context.ApplicationServices.GetRequiredService<ILoggerFactory>();
			var logger = loggerFactory.CreateLogger("NotFoundMiddleware");
            
            var telemetryClient = context.ApplicationServices.GetRequiredService<TelemetryClient>();

			await _next(context);
			
            if (context.Response.StatusCode == 404)
            {
                // if the request path looks like /yyyy/mm/dd/slug or /slug, redirect to the blog controller
    			var match = Regex.Match(context.Request.Path.Value ?? string.Empty, @"^/(?:(?<year>\d\d\d\d)/(?<month>\d\d?)/(?<day>\d\d?)/)?(?<slug>[_a-z0-0-]*)/?$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    logger.LogInformation("{Path}{QueryString} looks like a WP era URL. Redirecting to /blog{Path}", context.Request.Path, context.Request.QueryString, context.Request.Path);
                    context.Response.Redirect("/blog" + context.Request.Path);
                }
                else
                {
                    logger.LogWarning("{Path}{QueryString} Not Found", context.Request.Path, context.Request.QueryString);
                    telemetryClient.TrackEvent("404", new Dictionary<string, string> {{ "url", $"{context.Request.Path}{context.Request.QueryString}" }});
                }
            }

		}
    }
}