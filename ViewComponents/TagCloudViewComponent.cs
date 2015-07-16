using System;
using System.Linq;
using Microsoft.AspNet.Mvc;

namespace HawkProto2
{
    public class TagCloudViewComponent : ViewComponent
    {
        private readonly IPostRepository _repo;
        
        public TagCloudViewComponent(IPostRepository repo)
        {
            if (repo == null)
            {
                throw new ArgumentNullException(nameof(repo));
            }
            
            this._repo = repo;
        }
        
        public IViewComponentResult Invoke()
        {
            var tags = _repo.Tags().ToArray();
            return View(tags);
        }
    }
}