using System;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Hawk.Services;

namespace Hawk.ViewComponents
{
    public class TagCloudViewComponent : ViewComponent
    {
        readonly IPostRepository _repo;

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
            var tags = _repo.Tags().OrderBy(t => t.Item1.Title).ToArray();
            return View(tags);
        }
    }
}