using System;
using System.Linq;
using System.Text;
using System.Xml;
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
            Response.ContentType = "application/rss+xml";
            
            var rootUrl = new Uri("http://devhawk.net");
            
            using (var writer = new HttpResponseStreamWriter(Response.Body, Encoding.UTF8))
            {
                var settings = new XmlWriterSettings()
                {
                    Indent = true,
                    NewLineHandling = NewLineHandling.Entitize,
                };

                using (var xw =  XmlWriter.Create(writer, settings))
                {
                    xw.WriteStartElement("rss");
                    xw.WriteAttributeString("version", "2.0");
                    xw.WriteStartElement("channel");
                    xw.WriteElementString("title", "DevHawk");
                    xw.WriteElementString("link", rootUrl.ToString());
                    xw.WriteElementString("description", "Passion * Technology * Ruthless Competence");
                    
                    foreach (var post in _repo.Posts().Take(10))
                    {
                        xw.WriteStartElement("item");
                        xw.WriteElementString("title", post.Title);
                        
                        var postRelUrl = Url.Action("Post", "Home", new { year = post.Date.Year, month = post.Date.Month, day = post.Date.Day, slug = post.Slug });
                        var postAbsUrl = new Uri(rootUrl, postRelUrl);
                        xw.WriteElementString("link", postAbsUrl.ToString());
                        xw.WriteElementString("author", post.Author.Name); // TODO: author should be email address
                        xw.WriteElementString("pubdate", post.Date.ToString("r"));
                        foreach (var cat in post.Categories)
                        {
                            xw.WriteElementString("category", cat.Title);
                        }
                        foreach (var tag in post.Tags)
                        {
                            xw.WriteElementString("category", tag.Title);
                        }
                        xw.WriteStartElement("content", "encoded", "http://purl.org/rss/1.0/modules/content/");
                        xw.WriteCData(post.RenderedContent.Value);
                        xw.WriteEndElement(); // content:encoded
                        xw.WriteEndElement(); // item
                    }
                    
                    xw.WriteEndElement(); // channel
                    xw.WriteEndElement(); // rss
                }
            }

            return new EmptyResult();
        }

        //  public IActionResult Atom()
        //  {
        //      return Content("FeedController.Atom");
        //  }
        //  
        //  [RouteAttribute("atom")]
        //  public IActionResult RootAtom()
        //  {
        //      //redirect to /feed/atom
        //      return Atom();
        //  }
    }
}
