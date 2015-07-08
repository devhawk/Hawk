using System;
using Microsoft.AspNet.Mvc;

namespace HawkProto2
{
    public class FeedController : Controller
    {
        private readonly IPostRepository _repo;
        
        public FeedController(IPostRepository repo)
        {
            if (repo == null)
            {
                throw new ArgumentNullException(nameof(repo));
            }
            
            this._repo = repo;
        }
        
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
