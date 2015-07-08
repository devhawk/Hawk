using Microsoft.AspNet.Mvc;

namespace HawkProto2
{
    public class CategoryListViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return Content("CategoryListViewComponent");
        }
    }
}