using System;
using System.Linq;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
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

        [Route("refresh")]
        public IActionResult Refresh(string key)
        {
            var hostEnv = Context.ApplicationServices.GetService<IHostingEnvironment>();
            if (hostEnv == null)
            {
                _logger.LogError("Could not retrieve IHostingEnvironment instance from ApplicationServices");
                throw new Exception();
            }
                        
            // require https in production
            if (hostEnv.IsProduction() && !Request.IsHttps)
            {
                return HttpBadRequest();
            }

            var optionsAccessor = Context.ApplicationServices.GetService<IOptions<HawkOptions>>();
            if (optionsAccessor == null)
            {
                _logger.LogError("Could not retrieve IOptions<HawkOptions> instance from ApplicationServices");
                throw new Exception();
            }

            // make sure key parmeter matches configured value
            if (key != optionsAccessor.Options.RefreshKey)
            {
                return HttpBadRequest();
            }
            
            _logger.LogInformation("Refreshing content");

            var cache = Context.ApplicationServices.GetService<IMemoryCache>();

            var reloadContent = cache.Get<Action>("Hawk.ReloadContent");
            if (reloadContent != null)
            {
                reloadContent();
            }

            return RedirectToAction("Index", "Home");
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
