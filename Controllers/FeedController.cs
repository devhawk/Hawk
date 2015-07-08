using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HawkProto2
{
    public class FeedController : Controller
    {
        [RouteAttribute("feed")]
        public IActionResult Index()
        {
            return Rss();
        }
        
        public IActionResult Rss()
        {
            //  redirect /feed/rss to /feed
            return Content("FeedController.Rss");
        }

        public IActionResult Atom()
        {
            return Content("FeedController.Atom");
        }
        
        [RouteAttribute("atom")]
        public IActionResult RootAtom()
        {
            //redirect to /feed/atom
            return Atom();
        }
    }
}
