using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HawkProto2
{
    public class HomeController : Controller
    {
        
        [Route("")]
        public IActionResult Index()
        {
            return Index(1);
        }

        [Route("/page/{pageNum}")]
        public IActionResult Index(int pageNum)
        {
            return Content($"HomeController.Index {pageNum}");
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
    }
}
