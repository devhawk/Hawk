using System;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Logging;

namespace Hawk
{
    public class HomeController : Controller
    {
        const int PAGE_SIZE = 5;

        readonly IPostRepository _repo;
        readonly ILogger _logger;

        public HomeController(IPostRepository repo, ILoggerFactory loggerFactory)
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
            this._logger = loggerFactory.CreateLogger(nameof(HomeController));
        }

        [Route("")]
        public IActionResult Index()
        {
            var posts = _repo.Posts().Take(5).ToArray();
            return View(posts);
        }
    }
}
