using System;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.AspNet.Mvc;

namespace HawkProto2
{
    public class CompatController : Controller
    {
        private readonly IPostRepository _repo;
        
        public CompatController(IPostRepository repo)
        {
            if (repo == null)
            {
                throw new ArgumentNullException(nameof(repo));
            }
            
            this._repo = repo;
        }
        
        IActionResult GetPostAction(Post post)
        {
            if (post == null)
            {
                return HttpNotFound();
            }
            
            var url = Url.Action("Post", "Home", new {
                    year = post.Date.Year,
                    month = post.Date.Month,
                    day = post.Date.Day,
                    slug = post.Slug,
                });
            return new RedirectResult(url, true);
        }
        
        public IActionResult EntryId(Guid id)
        {
            return GetPostAction(_repo.PostByDasBlogEntryId(id));

        }
        
        public IActionResult Title(string id)
        {
            return GetPostAction(_repo.PostByDasBlogTitle(id));
        }
        
	}
}

