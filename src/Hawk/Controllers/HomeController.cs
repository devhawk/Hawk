using System;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Logging;
using Hawk.Services;

namespace Hawk.Controllers
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

        [Route("error")]
        public IActionResult Error()
        {
            return View();
        }

        IActionResult RedirectPost(Models.Post post)
        {
            if (post == null)
            {
                return HttpNotFound();
            }

            _logger.LogInformation($"{Request.Path} looks like a WP era URL. Redirecting to /blog{Request.Path}");

            return Redirect("/blog" + Request.Path);
        }

        [Route("{year:int}/{month:range(1,12)}/{day:range(1,31)}/{slug}")]
        public IActionResult Post(int year, int month, int day, string slug)
        {
            var post = _repo.Posts().FirstOrDefault(p => p.Date.Year == year && p.Date.Month == month && p.Date.Day == day && p.Slug == slug);
            return RedirectPost(post);
        }

        [Route("{slug}")]
        public IActionResult SlugPost(string slug)
        {
            var post = _repo.Posts().FirstOrDefault(p => p.Slug == slug);
            return RedirectPost(post);
        }
    }
}
