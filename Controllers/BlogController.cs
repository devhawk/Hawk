using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Logging;
using Hawk.Models;
using Hawk.Services;

namespace Hawk.Controllers
{
	[Route("blog")]
    public class BlogController : Controller
    {
        const int PAGE_SIZE = 5;

        readonly IPostRepository _repo;
        readonly ILogger _logger;

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
        
        void Log([CallerMemberName] string methodName = null)
        {
            _logger.LogInformation(methodName);
        }

        RouteValueDictionary GetRouteValues(int pageNum, object routeValues = null)
        {
            var routeValueDict = new RouteValueDictionary(routeValues);
            routeValueDict.Add("pageNum", pageNum);
            return routeValueDict;            
        }
        
        IActionResult PostsHelper(IEnumerable<Post> posts, int pageNum, string action, object routeValues = null)
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
                return View("MultiplePosts", posts.ToArray());
                //return HttpNotFound();
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

            // generate previous / next page link URLs
            // If we're already on the first/last page, then don't generate prev/next page URLs
            // If we're on page 2, generate the prev page link to the root action instead of action/page/1             
            
            ViewBag.NewerPostsLink = pageNum == 1 ? string.Empty : (pageNum == 2 ? Url.Action(action, routeValues) : Url.Action(actionPage, GetRouteValues(pageNum - 1, routeValues))); 
            ViewBag.OlderPostsLink = pageNum == pageCount ? string.Empty : Url.Action(actionPage, GetRouteValues(pageNum + 1, routeValues));
            
            return View("MultiplePosts", pagePosts);
        }   

        public IActionResult Index()
        {
            Log();
            return IndexPage(1);
        }
        
        [Route("archives")]
        public IActionResult Archives()
        {
            var posts = _repo.Posts().ToArray();
            return View(posts);
        }

        [Route("page/{pageNum}")]
        public IActionResult IndexPage(int pageNum)
        {
            Log();
            ViewBag.Title = "Blog Home";
            return PostsHelper(_repo.Posts(), pageNum, "Index");
        }

        [Route("{year:int}")]
        public IActionResult PostsByYear(int year)
        {
            return PostsByYearPage(year, 1);
        }
        
        [Route("{year:int}/page/{pageNum:int}")]
        public IActionResult PostsByYearPage(int year, int pageNum)
        {
            Log();
            
            ViewBag.Title = $"Posts from ({year})";
            ViewBag.PageHeader = $"Posts from {year}"; 
            return PostsHelper(_repo.Posts().Where(p => p.Date.Year == year), pageNum, "PostsByYear", new { year });
        }

        [Route("{year:int}/{month:range(1,12)}")]
        public IActionResult PostsByMonth(int year, int month)
        {
            return PostsByMonthPage(year, month, 1);
        }

        [Route("{year:int}/{month:range(1,12)}/page/{pageNum}")]
        public IActionResult PostsByMonthPage(int year, int month, int pageNum)
        {
            Log();

            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
            ViewBag.Title = $"Posts from ({year}-{month})";
            ViewBag.PageHeader = $"Posts from {monthName} {year}"; 

            return PostsHelper(
                _repo.Posts().Where(p => p.Date.Year == year && p.Date.Month == month), 
                pageNum, "PostsByMonth", new { year, month });
        }

        [Route("{year:int}/{month:range(1,12)}/{day:range(1,31)}")]
        public IActionResult PostsByDay(int year, int month, int day)
        {
            return PostsByDayPage(year, month, day, 1);
        }

        [Route("{year:int}/{month:range(1,12)}/{day:range(1,31)}/page/{pageNum}")]
        public IActionResult PostsByDayPage(int year, int month, int day, int pageNum)
        {
            Log();
            
            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
            ViewBag.Title = $"Posts from ({year}-{month}-{day})";
            ViewBag.PageHeader = $"Posts from {monthName} {day}, {year}"; 

            return PostsHelper(
                _repo.Posts().Where(p => p.Date.Year == year && p.Date.Month == month && p.Date.Day == day), 
                pageNum, "PostsByMonth", new { year, month, day });
        }
        
        string GeneratePostUrl(Post post)
        {
            return Url.Action("Post", new {year = post.Date.Year, month = post.Date.Month, day = post.Date.Day, slug = post.Slug});
        }

        [Route("{year:int}/{month:range(1,12)}/{day:range(1,31)}/{slug}")]
        public IActionResult Post(int year, int month, int day, string slug)
        {
            Log();
            
            var post = _repo.Posts().FirstOrDefault(p => p.Date.Year == year && p.Date.Month == month && p.Date.Day == day && p.Slug == slug);
            if (post == null)            
            {
                return HttpNotFound();
            }

            ViewBag.Title = $"- {post.Title}";
            return View("Post", post);
        }

        [Route("{slug}")]
        public IActionResult SlugPost(string slug)
        {
            Log();
            var post = _repo.Posts().FirstOrDefault(p => p.Slug == slug);
            if (post == null)
            {
                return HttpNotFound();
            }
            
            return RedirectToAction("Post", new { year = post.Date.Year, month = post.Date.Month, day = post.Date.Day, slug } );
        }
        
        [Route("category/{name}")]
        public IActionResult Category(string name)
        {
            return CategoryPage(name, 1);
        }

        [Route("category/{name}/page/{pageNum}")]
        public IActionResult CategoryPage(string name, int pageNum)
        {
            Log();
            
            var title = _repo.Categories()
                .Where(s => s.Item1.Slug == name)
                .Select(s => s.Item1.Title)
                .Single();

            ViewBag.Title = $"({title}) Posts";
            ViewBag.PageHeader = $"{title} Posts";
             
            return PostsHelper(
                _repo.Posts().Where(p => p.Categories.Any(c => c.Slug == name)), 
                pageNum, "Category", new { name });
        }
        
        [Route("tag/{name}")]
        public IActionResult Tag(string name)
        {
            return TagPage(name, 1);
        }

        [Route("tag/{name}/page/{pageNum}")]
        public IActionResult TagPage(string name, int pageNum)
        {
            Log();
            
            var title = _repo.Tags()
                .Where(s => s.Item1.Slug == name)
                .Select(s => s.Item1.Title)
                .Single();
            ViewBag.Title = $"({title}) Posts";
            ViewBag.PageHeader = $"{title} Posts";
             
            return PostsHelper(
                _repo.Posts().Where(p => p.Tags.Any(c => c.Slug == name)), 
                pageNum, "Tag", new { name });
        }
        
        [Route("author/{slug}")]
        public IActionResult Author(string slug)
        {
            return AuthorPage(slug, 1);    
        }
        
        [Route("author/{slug}/page/{pageNum}")]
        public IActionResult AuthorPage(string slug, int pageNum)
        {
            Log();
            
            var name = _repo.Posts()
                .Where(p => p.Author.Slug == slug)
                .Select(p => p.Author.Name)
                .First();
            ViewBag.Title = $"Posts by ({name})";
            ViewBag.PageHeader = $"Posts by {name}"; 

            return PostsHelper(
                _repo.Posts().Where(p => p.Author.Slug == slug), 
                pageNum, "Author", new { slug });
        }
	}
}