using System;

using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Logging;

namespace HawkProto2
{
    public class CompatController : Controller
    {
        private readonly IPostRepository _repo;
        private readonly ILogger _logger;
        
        public CompatController(IPostRepository repo, ILoggerFactory loggerFactory)
        {
            if (repo == null)
            {
                throw new ArgumentNullException(nameof(repo));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }
            
            this._repo = repo;
            this._logger = loggerFactory.CreateLogger("CompatController");
        }
        
        IActionResult GetPostAction(Post post)
        {
            if (post == null)
            {
                return HttpNotFound();
            }
            
            var url = Url.Action("Post", "Home", new {
                    year = post.Date.Year,
                    month = post.Date.Month,
                    day = post.Date.Day,
                    slug = post.Slug,
                });
                
            _logger.LogInformation("Redirecting {Path}{Query} to {url}", Request.Path, Request.Query, url);
            return new RedirectResult(url, true);
        }
        
        public IActionResult EntryId(Guid id)
        {
            return GetPostAction(_repo.PostByDasBlogEntryId(id));

        }
        
        public IActionResult Title(string id)
        {
            return GetPostAction(_repo.PostByDasBlogTitle(id));
        }
        
	}
}

