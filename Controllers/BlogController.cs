using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Framework.Logging;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;

namespace HawkProto2
{
	[Route("blog")]
    public class BlogController : Controller
    {
        const int PAGE_SIZE = 5;

        private readonly IPostRepository _repo;
        private readonly ILogger _logger;
        
        public BlogController(IPostRepository repo, ILoggerFactory loggerFactory)
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
            this._logger = loggerFactory.CreateLogger(nameof(BlogController));
        }
        
        private void Log([CallerMemberName] string methodName = null)
        {
            _logger.LogInformation(methodName);
        }

        private RouteValueDictionary GetRouteValues(int pageNum, object routeValues = null)
        {
            var routeValueDict = new RouteValueDictionary(routeValues);
            routeValueDict.Add("pageNum", pageNum);
            return routeValueDict;            
        }
        
        private IActionResult PostsHelper(IEnumerable<Post> posts, int pageNum, string action, object routeValues = null)
        {
            // if the user asks for a page less than or equal to zero, redirect to the first page
            if (pageNum <= 0)
            {
                return RedirectToAction(action);
            }
             
            // if there are no posts, return 404
            var postCount = posts.Count();
            if (postCount == 0)
            {
                return HttpNotFound();
            }
            
            // if the user asks for a page beyond the last page, redirect to the last page
            var pageCount = postCount / PAGE_SIZE + (postCount % PAGE_SIZE == 0 ? 0 : 1);
            if (pageNum > pageCount)
            {
                return RedirectToAction(action + "Page", GetRouteValues(pageCount, routeValues));
            }
            
            var actionPage = action + "Page";
            var skip = (pageNum - 1) * PAGE_SIZE;
            var pagePosts = posts.Skip(skip).Take(PAGE_SIZE).ToArray();
            
            ViewBag.PrevNextPageLinks = Tuple.Create(
                pageNum == 1 ? string.Empty : pageNum == 2 ? Url.Action(action, routeValues) : Url.Action(actionPage, GetRouteValues(pageNum - 1, routeValues)),
                pageNum == pageCount ? string.Empty : Url.Action(actionPage, GetRouteValues(pageNum + 1, routeValues)));
            
            return View("Index", pagePosts);
        }   

        //  [Route("")]
        public IActionResult Index()
        {
            Log();
            return IndexPage(1);
        }

        [Route("page/{pageNum}")]
        public IActionResult IndexPage(int pageNum)
        {
            Log();
            return PostsHelper(_repo.Posts(), pageNum, "Index");
        }

        [Route("{year:int}")]
        public IActionResult PostsByYear(int year)
        {
            Log();
            return PostsByYearPage(year, 1);
        }
        
        [Route("{year:int}/page/{pageNum:int}")]
        public IActionResult PostsByYearPage(int year, int pageNum)
        {
            Log();
            return PostsHelper(_repo.Posts().Where(p => p.Date.Year == year), pageNum, "PostsByYear", new { year = year });
        }

        [Route("{year:int}/{month:range(1,12)}")]
        public IActionResult PostsByMonth(int year, int month)
        {
            Log();
            return PostsByMonthPage(year, month, 1);
        }

        [Route("{year:int}/{month:range(1,12)}/page/{pageNum}")]
        public IActionResult PostsByMonthPage(int year, int month, int pageNum)
        {
            Log();
            return PostsHelper(
                _repo.Posts().Where(p => p.Date.Year == year && p.Date.Month == month), 
                pageNum, "PostsByMonth", new { year = year, month = month });
        }

        [Route("{year:int}/{month:range(1,12)}/{day:range(1,31)}")]
        public IActionResult PostsByDay(int year, int month, int day)
        {
            Log();
            return PostsByDayPage(year, month, day, 1);
        }

        [Route("{year:int}/{month:range(1,12)}/{day:range(1,31)}/page/{pageNum}")]
        public IActionResult PostsByDayPage(int year, int month, int day, int pageNum)
        {
            Log();
            return PostsHelper(
                _repo.Posts().Where(p => p.Date.Year == year && p.Date.Month == month && p.Date.Day == day), 
                pageNum, "PostsByMonth", new { year = year, month = month, day = day });
        }

        [Route("{year:int}/{month:range(1,12)}/{day:range(1,31)}/{slug}")]
        public IActionResult Post(int year, int month, int day, string slug)
        {
            Log();
            var post = _repo.Posts().Where(p => p.Date.Year == year && p.Date.Month == month && p.Date.Day == day && p.Slug == slug).FirstOrDefault();
            if (post == null)
            {
                return HttpNotFound();
            }
            
            return View("Post", post);
        }

        [Route("{slug}")]
        public IActionResult SlugPost(string slug)
        {
            Log();
            var post = _repo.Posts().Where(p => p.Slug == slug).FirstOrDefault();
            if (post == null)
            {
                return HttpNotFound();
            }
            
            return RedirectToActionPermanent("Post", new { year = post.Date.Year, month = post.Date.Month, day = post.Date.Day, slug = slug } );
        }
        
        [Route("category/{name}")]
        public IActionResult Category(string name)
        {
            Log();
            return CategoryPage(name, 1);
        }

        [Route("category/{name}/page/{pageNum}")]
        public IActionResult CategoryPage(string name, int pageNum)
        {
            Log();
            return PostsHelper(
                _repo.Posts().Where(p => p.Categories.Any(c => c.Slug == name)), 
                pageNum, "Category", new { name = name });
        }
        
        [Route("tag/{name}")]
        public IActionResult Tag(string name)
        {
            Log();
            return TagPage(name, 1);
        }

        [Route("tag/{name}/page/{pageNum}")]
        public IActionResult TagPage(string name, int pageNum)
        {
            Log();
            return PostsHelper(
                _repo.Posts().Where(p => p.Tags.Any(c => c.Slug == name)), 
                pageNum, "Tag", new { name = name });
        }
        
        [Route("author/{name}")]
        public IActionResult Author(string name)
        {
            Log();
            return AuthorPage(name, 1);    
        }
        
        [Route("author/{name}/page/{pageNum}")]
        public IActionResult AuthorPage(string name, int pageNum)
        {
            Log();
            return PostsHelper(
                _repo.Posts().Where(p => p.Author.Slug == name), 
                pageNum, "Author", new { name = name });
        }
	}
}