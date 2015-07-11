using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

namespace HawkProto2
{
	delegate string Redirector(string path, Func<string, string> query);
	
    static class RedirectorExtensions
    {
        public static Redirector Bind(this Redirector r1, Redirector r2)
        {
            return (path, query) => r1(path, query) ?? r2(path, query);
        }
    }

	public class DasBlogRedirector
	{
	    RequestDelegate _next;
	
	    public DasBlogRedirector(RequestDelegate next)
	    {
	        _next = next;
	    }
		
		public static bool CompareIgnoreCase(string s1, string s2)
        {
            return string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static IEnumerable<string> Match(string pattern, string input)
        {
			var match = Regex.Match(input ?? string.Empty, pattern, RegexOptions.IgnoreCase);
            return match.Success ?  match.Groups.Cast<Group>().Skip(1).Select(g => g.Value) : null;
        }

		public static string Match(string pattern, string input, string output)
		{
			var matches = Match(pattern, input);
			return matches != null ? string.Format(output, matches.ToArray()) : null; 
		}

		static Redirector PathCompare(string expectedPath, string output)
        {
            return (path, query) => CompareIgnoreCase(path, expectedPath) ? output : null;
        } 
		
		static Redirector PathMatch(string pattern, string output)
        {
            return (path, query) => Match(pattern, path, output);
        }

        static Redirector PathQueryMatch(string expectedPath, string name, string pattern, string output)
        {
            return (path, query) =>
            {
                if (!CompareIgnoreCase(path, expectedPath))
                    return null;
				
                var matches = Match(pattern, query(name));
                if (matches == null)
                    return null;

                return string.Format(output, matches.ToArray());
            };
        }

        static Redirector PathQueryMatch(string expectedPath, string name, string output)
        {
            return (path, query) =>
            {
                if (string.Compare(path, expectedPath, StringComparison.OrdinalIgnoreCase) != 0)
                    return null;

                var value = query(name);
                if (value == null)
                    return null;

                return string.Format(output, value);
            };
        }
		
		static Redirector GetRedirector(ILogger logger)
		{
            Redirector defaultaspx = (path, query) => 
                {
                    if (!CompareIgnoreCase("/default.aspx", path))
                        return null;

                    var matches = Match(@"^(\d\d\d\d)-(\d\d?)-(\d\d?)$", query("date"));
                    if (matches != null)
                        return string.Format("/{0}/{1}/{2}", matches.ToArray());

                    matches = Match(@"^(\d\d\d\d)-(\d\d?)$", query("month"));
                    if (matches != null)
                        return string.Format("/{0}/{1}", matches.ToArray());

                    return "/";
                };
				 
			return defaultaspx
				.Bind(PathCompare("/archives.aspx", "/archives"))
				.Bind(PathCompare("/rss.aspx", "/feed/rss"))
				.Bind(PathCompare("/atom.aspx", "/feed/atom"))
				.Bind(PathMatch(@"^/default,date,(\d{4})-(\d\d?)-(\d\d?)\.aspx$", "/{0}/{1}/{2}"))
				.Bind(PathMatch(@"^/default,month,(\d{4})-(\d\d?)\.aspx$", "/{0}/{1}"))
				.Bind(PathMatch(@"^/monthview,month,(\d{4})-(\d\d?)\.aspx$", "/{0}/{1}"))
				.Bind(PathMatch(@"^/monthview,year,(\d{4})\.aspx$", "/{0}"))
				.Bind(PathMatch(@"^/CategoryView,category,(.*)\.aspx$", "/category/{0}"))
				.Bind(PathQueryMatch("/monthview.aspx", "month", @"^(\d{4})-(\d\d?)$", "/{0}/{1}"))
				.Bind(PathQueryMatch("/monthview.aspx", "year", @"^(\d{4})$", "/{0}"))
				
				//  .Bind(PathMatch(@"^/SearchView,q,(.*)\.aspx$", "/search?q={0}"))
				//  .Bind(PathQueryMatch("/SearchView.aspx", "q", "/search?q={0}"))
				//  .Bind(PathQueryMatch("/SearchView.aspx", "category", "/category/{0}"))
				
				.Bind(PathQueryMatch("/PermaLink.aspx", "guid", @"^([a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12})$", "/compat/entryId/{0}"))
				.Bind(PathMatch(@"^/PermaLink,guid,([a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12})\.aspx$", "/compat/entryId/{0}"))
				.Bind(PathQueryMatch("/CommentView.aspx", "guid", @"^([a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12})$", "/compat/entryId/{0}"))
				.Bind(PathMatch(@"^/CommentView,guid,([a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12})\.aspx$", "/compat/entryId/{0}"))
				//  .Bind(PathMatch(@"^/(\d{4})/(\d\d?)/(\d\d?)/(.*)\.aspx$", "/compat/title={3}&year={0}&month={1}&day={2}"))
				.Bind(PathMatch(@"^/\d{4}/\d\d?/\d\d?/(.*)\.aspx$", "/compat/title/{0}"))
				.Bind(PathMatch(@"^/(.*)\.aspx$", "/compat/title/{0}"))
				;			
		}
						
		static string GetRedirectUrl(HttpRequest req, ILogger logger)
		{
			// All dasBlog urls end with .aspx. Bail out immediately for URLs that don't end in .aspx
			if (req.Path.HasValue && req.Path.Value.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
			{
				
                return GetRedirector(logger)(req.Path.Value, req.Query.Get);
			}

			return null;			
		} 
	
	    public async Task Invoke(HttpContext context)
	    {
			var loggerFactory = context.ApplicationServices.GetRequiredService<ILoggerFactory>();
			var logger = loggerFactory.CreateLogger("DevHawkRedirector");
						
			//  logger.LogError(context.Request.Path.Value);

			var redirectUrl = GetRedirectUrl(context.Request, logger);
			
			if (string.IsNullOrEmpty(redirectUrl))
			{
				await _next(context);
			}
			else
			{
				logger.LogInformation("Redirecting {Path}{QueryString} to {redirectUrl}", context.Request.Path, context.Request.QueryString, redirectUrl);
				context.Response.Redirect(redirectUrl, true);
			}
		}
	}
}
                