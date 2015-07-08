using Microsoft.AspNet.Mvc;

namespace HawkProto2
{
    public class TagCloudViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return Content("TagCloudViewComponent");
        }
    }
}