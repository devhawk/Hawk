using System;
using System.Linq;
using Microsoft.AspNet.Mvc;

namespace HawkProto2
{
    public class HomeController : Controller
    {
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
            return Index(1);
        }

        const int PAGE_SIZE = 5;
        
        [Route("/page/{pageNum}")]
        public IActionResult Index(int pageNum)
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
            
            var posts = _repo.AllPosts().Skip(skip).Take(PAGE_SIZE);
            
            //  return Content($"HomeController.Index {pageNum}");
            return View(posts);
        }
        
        [Route("archive")]
        public IActionResult Archive()
        {
            return Content("HomeController.Archive");
        }
        
        [Route("about")]
        public IActionResult About()
        {
            return Content("HomeController.About");
        }
        
        [Route("{year:int}")]
        public IActionResult PostsByYear(int year)
        {
            return PostsByYear(year, 1);
        }
        
        [Route("{year:int}/page/{pageNum:int}")]
        public IActionResult PostsByYear(int year, int pageNum)
        {
            //TODO add date constraints
            return Content($"HomeController.PostsByYear {year} page #{pageNum}");
        }

        [Route("{year:int}/{month:int}")]
        public IActionResult PostsByMonth(int year, int month)
        {
            return PostsByMonth(year, month, 1);
        }

        [Route("{year:int}/{month:int}/page/{pageNum}")]
        public IActionResult PostsByMonth(int year, int month, int pageNum)
        {
            //TODO add date constraints
            return Content($"HomeController.PostsByMonth {year}-{month} page #{pageNum}");
        }

        [Route("{year:int}/{month:int}/{day:int}")]
        public IActionResult PostsByDay(int year, int month, int day)
        {
            return PostsByDay(year, month, day, 1);
        }

        [Route("{year:int}/{month:int}/{day:int}/page/{pageNum}")]
        public IActionResult PostsByDay(int year, int month, int day, int pageNum)
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
            return Category(name, 1);
        }

        [Route("category/{name}/page/{pageNum}")]
        public IActionResult Category(string name, int pageNum)
        {
            return Content($"HomeController.Category {name} page #{pageNum}");
        }
        
        [Route("tag/{name}")]
        public IActionResult Tag(string name)
        {
            return Tag(name, 1);
        }

        [Route("tag/{name}/page/{pageNum}")]
        public IActionResult Tag(string name, int pageNum)
        {
            return Content($"HomeController.Tag {name} page #{pageNum}");
        }
        
        [Route("author/{name}")]
        public IActionResult Author(string name)
        {
            return Author(name, 1);    
        }
        
        [Route("author/{name}/page/{pageNum}")]
        public IActionResult Author(string name, int pageNum)
        {
            return Content($"HomeController.Author {name} page #{pageNum}");
        }
        
    }
}
