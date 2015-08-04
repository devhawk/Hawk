using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

namespace Hawk
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

		// Helper function to do OrdinalIgnoreCase comparison of two strings 		
		public static bool CompareIgnoreCase(string s1, string s2)
        {
            return string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) == 0;
        }
		
		// Helper function to do a RegEx match, returning an IEnumerable of strings on success or null on failure
        public static IEnumerable<string> Match(string pattern, string input)
        {
			var match = Regex.Match(input ?? string.Empty, pattern, RegexOptions.IgnoreCase);
            return match.Success ?  match.Groups.Cast<Group>().Skip(1).Select(g => g.Value) : null;
        }

		// Helper function to do a RegEx match and apply a function to generate the output
		public static string Match(string pattern, string input, Func<IEnumerable<string>, string> outputFunc)
		{
			var matches = Match(pattern, input);
			return matches == null ? null : outputFunc(matches);
		}
		
		//  // helper function to do a regex match and string.format the output
		//  public static string Match(string pattern, string input, string output)
		//  {
		//  	return Match(pattern, input, matches => string.Format(output, matches.ToArray()));
		//  }

		// Redirector generator that returns the output parameter when the request path matches the expectedPath paremeter
		static Redirector PathCompare(string expectedPath, string output)
        {
            return (path, query) => CompareIgnoreCase(path, expectedPath) ? output : null;
        } 
		
		// Redirector generator that matches the request path against a regex and applies the outputFunc to the match collection 
		static Redirector PathMatch(string pattern, Func<IEnumerable<string>, string> outputFunc)
        {
            return (path, query) => Match(pattern, path, outputFunc);
        }

		// PathMatch helper that uses string.Format as the outputFunc
		static Redirector PathMatch(string pattern, string output)
        {
            return PathMatch(pattern, matches => string.Format(output, matches.ToArray()));
        }

		// Redirector generator that matches the request path against the expectedPath parameter 
		// and a named query string value against a regex and applies the outputFunc to the query 
		// string regex match collection
        static Redirector PathQueryMatch(string expectedPath, string name, string pattern, Func<IEnumerable<string>, string> outputFunc)
        {
            return (path, query) =>
            {
                if (!CompareIgnoreCase(path, expectedPath))
                    return null;
				
                var matches = Match(pattern, query(name));
                if (matches == null)
                    return null;

                return outputFunc(matches);
            };
        }

		// PathQueryMatch helper that uses string.Format as the outputFunc
        static Redirector PathQueryMatch(string expectedPath, string name, string pattern, string output)
        {
			return PathQueryMatch(expectedPath, name, pattern, matches => string.Format(output, matches.ToArray()));
        }

		static string FormatPostUrl(Post post)
		{
			return post == null ? null : $"/blog/{post.Date.ToString("yyyy/MM/dd")}/{post.Slug}";
		}
		
		static string GetPostUrlByEntryId(IPostRepository repo, IEnumerable<string> matches)
		{
			var match = matches.Single();
			var entryId = Guid.Parse(match);
			var post = repo.PostByDasBlogEntryId(entryId);
			
			return FormatPostUrl(post);
		}

		static string GetPostUrlByTitle(IPostRepository repo, IEnumerable<string> matches)
		{
			Post post;
			switch (matches.Count())
			{
				case 1:
				{
					var slug = matches.Single();
					post = repo.PostByDasBlogTitle(slug);
					break;
				}
				case 4:
				{
					var intArray = matches.Take(3).Select(t => int.Parse(t)).ToArray();
					var date = new DateTimeOffset(intArray[0], intArray[1], intArray[2], 0, 0, 0, TimeSpan.Zero);
					var slug = matches.ElementAt(3);
					post = repo.PostByDasBlogTitle(slug, date.Date);
					break;
				}
				default:
					throw new ArgumentException("matches had the wrong number of elements");					
			}

			return FormatPostUrl(post);
		}

		static Redirector GetRedirector(ILogger logger, IPostRepository repo)
		{
			if (repo == null)
			{
				throw new ArgumentNullException("repo");
			}

            Redirector defaultaspx = (path, query) => 
                {
                    if (!CompareIgnoreCase("/default.aspx", path))
                        return null;

                    var matches = Match(@"^(\d\d\d\d)-(\d\d?)-(\d\d?)$", query("date"));
                    if (matches != null)
                        return string.Format("/blog/{0}/{1}/{2}", matches.ToArray());

                    matches = Match(@"^(\d\d\d\d)-(\d\d?)$", query("month"));
                    if (matches != null)
                        return string.Format("/blog/{0}/{1}", matches.ToArray());

                    return "/";
                };

			return defaultaspx
				.Bind(PathCompare("/archives.aspx", "/blog/archives"))
				.Bind(PathCompare("/rss.aspx", "/feed/rss"))
				.Bind(PathCompare("/atom.aspx", "/feed/atom"))
				.Bind(PathMatch(@"^/default,date,(\d{4})-(\d\d?)-(\d\d?)\.aspx$", "/blog/{0}/{1}/{2}"))
				.Bind(PathMatch(@"^/default,month,(\d{4})-(\d\d?)\.aspx$", "/blog/{0}/{1}"))
				.Bind(PathMatch(@"^/monthview,month,(\d{4})-(\d\d?)\.aspx$", "/blog/{0}/{1}"))
				.Bind(PathMatch(@"^/monthview,year,(\d{4})\.aspx$", "/blog/{0}"))
				.Bind(PathQueryMatch("/monthview.aspx", "month", @"^(\d{4})-(\d\d?)$", "/blog/{0}/{1}"))
				.Bind(PathQueryMatch("/monthview.aspx", "year", @"^(\d{4})$", "/blog/{0}"))
				//  .Bind(PathMatch(@"^/CategoryView,category,(.*)\.aspx$", "/blog/category/{0}"))
				//  .Bind(PathQueryMatch("/CategoryView.aspx", "category", @"^(.*)$", "/blog/category/{0}"))
				
				//  .Bind(PathMatch(@"^/SearchView,q,(.*)\.aspx$", "/search?q={0}"))
				//  .Bind(PathQueryMatch("/SearchView.aspx", "q", "/search?q={0}"))
				//  .Bind(PathQueryMatch("/SearchView.aspx", "category", "/category/{0}"))
				
				.Bind(PathQueryMatch("/PermaLink.aspx", "guid", @"^([a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12})$", m => GetPostUrlByEntryId(repo, m)))
				.Bind(PathMatch(@"^/PermaLink,guid,([a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12})\.aspx$", m => GetPostUrlByEntryId(repo, m)))
				.Bind(PathQueryMatch("/CommentView.aspx", "guid", @"^([a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12})$", m => GetPostUrlByEntryId(repo, m)))
				.Bind(PathMatch(@"^/CommentView,guid,([a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12})\.aspx$", m => GetPostUrlByEntryId(repo, m)))
				.Bind(PathMatch(@"^/(.*)\.aspx$", m => GetPostUrlByTitle(repo, m)))
				.Bind(PathMatch(@"^/(\d{4})/(\d\d?)/(\d\d?)/(.*)\.aspx$", m => GetPostUrlByTitle(repo, m)))
				;			
		}
						
		static string GetRedirectUrl(HttpRequest req, ILogger logger)
		{
			// All dasBlog urls end with .aspx. Bail out immediately for URLs that don't end in .aspx
			if (req.Path.HasValue && req.Path.Value.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
			{
				var repo = req.HttpContext.ApplicationServices.GetRequiredService<IPostRepository>();

				logger.LogInformation("Checking {Path}{QueryString}", req.Path, req.QueryString);
                var redirectUrl = GetRedirector(logger, repo)(req.Path.Value, req.Query.Get);
				
				if (redirectUrl == null)
				{
					logger.LogError("Could not match {Path}{QueryString}", req.Path, req.QueryString);
				}
				
				return redirectUrl;
			}

			return null;			
		} 
	
	    public async Task Invoke(HttpContext context)
	    {
			var loggerFactory = context.ApplicationServices.GetRequiredService<ILoggerFactory>();
			var logger = loggerFactory.CreateLogger("DevHawkRedirector");

			var redirectUrl = GetRedirectUrl(context.Request, logger);
			
			if (string.IsNullOrEmpty(redirectUrl))
			{
				await _next(context);
			}
			else
			{
				logger.LogInformation("Redirecting {Path}{QueryString} to {redirectUrl}", context.Request.Path, context.Request.QueryString, redirectUrl);
				context.Response.Redirect(redirectUrl);
			}
		}
	}
}
                