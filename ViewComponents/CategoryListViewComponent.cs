using System;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Hawk.Services;

namespace Hawk.ViewComponents
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