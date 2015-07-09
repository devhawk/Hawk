using System;
using System.Linq;
using Microsoft.Framework.Logging;
using Microsoft.AspNet.Mvc;

namespace HawkProto2
{
    public class HomeController : Controller
    {
        const int PAGE_SIZE = 5;

        private readonly IPostRepository _repo;
        
        public HomeController(IPostRepository repo)
        {
            if (repo == null)
            {
                throw new ArgumentNullException(nameof(repo));
            }
            
            this._repo = repo;
        }

        [Route("")]
        public IActionResult Index()
        {
            return IndexPage(1);
        }
                
        [Route("/page/{pageNum}")]
        public IActionResult IndexPage(int pageNum)
        {
            int skip = 0;
            if (pageNum > 0)
            {
                skip = (pageNum - 1) * PAGE_SIZE;
            }
            else
            {
                return RedirectToAction("Index");
            }
            
            var posts = _repo.AllPosts().Skip(skip).Take(PAGE_SIZE).ToArray();
            var totalPostCount = _repo.AllPosts().Count();
            var lastPage = totalPostCount / PAGE_SIZE + (totalPostCount % PAGE_SIZE == 0 ? 0 : 1);
            
            if (totalPostCount == 0)
                return new HttpNotFoundResult();
            
            if (posts.Length == 0)
            {
                return RedirectToAction("IndexPage", new { pageNum = lastPage });
            }    
            return View("Index", posts);
        }
        
        [Route("archive")]
        public IActionResult Archive()
        {
            var posts = _repo.AllPosts().ToArray();
            return View(posts);
        }
        
        [Route("about")]
        public IActionResult About()
        {
            return Content("HomeController.About");
        }
        
        [Route("{year:int}")]
        public IActionResult PostsByYear(int year)
        {
            return PostsByYearPage(year, 1);
        }
        
        [Route("{year:int}/page/{pageNum:int}")]
        public IActionResult PostsByYearPage(int year, int pageNum)
        {
            //TODO add date constraints
            return Content($"HomeController.PostsByYear {year} page #{pageNum}");
        }

        [Route("{year:int}/{month:int}")]
        public IActionResult PostsByMonth(int year, int month)
        {
            return PostsByMonthPage(year, month, 1);
        }

        [Route("{year:int}/{month:int}/page/{pageNum}")]
        public IActionResult PostsByMonthPage(int year, int month, int pageNum)
        {
            //TODO add date constraints
            return Content($"HomeController.PostsByMonth {year}-{month} page #{pageNum}");
        }

        [Route("{year:int}/{month:int}/{day:int}")]
        public IActionResult PostsByDay(int year, int month, int day)
        {
            return PostsByDayPage(year, month, day, 1);
        }

        [Route("{year:int}/{month:int}/{day:int}/page/{pageNum}")]
        public IActionResult PostsByDayPage(int year, int month, int day, int pageNum)
        {
            //TODO add date constraints
            return Content($"HomeController.PostsByDay {year}-{month}-{day} page #{pageNum}");
        }

        [Route("{year:int}/{month:int}/{day:int}/{slug}")]
        public IActionResult Post(int year, int month, int day, string slug)
        {
            //TODO add date constraints
            return Content($"HomeController.Post {year}-{month}-{day} {slug}");
        }

        [Route("{slug}")]
        public IActionResult Post(string slug)
        {
            //TODO add date constraints
            return Content($"HomeController.Post {slug}");
        }
        
        [Route("category/{name}")]
        public IActionResult Category(string name)
        {
            return CategoryPage(name, 1);
        }

        [Route("category/{name}/page/{pageNum}")]
        public IActionResult CategoryPage(string name, int pageNum)
        {
            return Content($"HomeController.Category {name} page #{pageNum}");
        }
        
        [Route("tag/{name}")]
        public IActionResult Tag(string name)
        {
            return TagPage(name, 1);
        }

        [Route("tag/{name}/page/{pageNum}")]
        public IActionResult TagPage(string name, int pageNum)
        {
            return Content($"HomeController.Tag {name} page #{pageNum}");
        }
        
        [Route("author/{name}")]
        public IActionResult Author(string name)
        {
            return AuthorPage(name, 1);    
        }
        
        [Route("author/{name}/page/{pageNum}")]
        public IActionResult AuthorPage(string name, int pageNum)
        {
            return Content($"HomeController.Author {name} page #{pageNum}");
        }
        
    }
}
