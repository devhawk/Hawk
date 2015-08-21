using System;
using System.Linq;
using Microsoft.AspNet.Mvc;

namespace Hawk
{
    public class CategoryListViewComponent : ViewComponent
    {
        readonly IPostRepository _repo;

        public CategoryListViewComponent(IPostRepository repo)
        {
            if (repo == null)
            {
                throw new ArgumentNullException(nameof(repo));
            }
            
            this._repo = repo;
        }

        public IViewComponentResult Invoke()
        {
            var cats = _repo.Categories().OrderBy(t => t.Item1.Title).ToArray();
            return View(cats);
        }
    }
}