using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNet.Mvc;
using Microsoft.ApplicationInsights;

namespace Hawk
{
    class TextWriterResult : IActionResult
    {
        readonly Func<TextWriter, Task> _func;

        public string ContentType { get; }

        public TextWriterResult(string contentType, Func<TextWriter, Task> func)
        {
            ContentType = contentType;
            _func = func;
        }
        
        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.ContentType = ContentType;
            
            using (var writer = new HttpResponseStreamWriter(response.Body, Encoding.UTF8))
            {
                await _func(writer);
            }
        }
    }

    public class FeedController : Controller
    {
        readonly IPostRepository _repo;
        readonly TelemetryClient _telemetryClient;

        public FeedController(IPostRepository repo, TelemetryClient telemetryClient)
        {
            if (repo == null)
            {
                throw new ArgumentNullException(nameof(repo));
            }
            
            if (telemetryClient == null)
            {
                throw new ArgumentNullException(nameof(telemetryClient));
            }
            
            this._repo = repo;
            this._telemetryClient = telemetryClient;
        }
        
        [RouteAttribute("feed")]
        public IActionResult Index()
        {
            return Rss();
        }

        public IActionResult Rss()
        {
            return new TextWriterResult("application/rss+xml", RssAsync);
        }

        async Task RssAsync(TextWriter writer)
        {
            _telemetryClient.TrackPageView($"{Request.Path}{Request.QueryString}");

            var rootUrl = new Uri(Request.Scheme + "://" + Request.Host.ToUriComponent());

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

                xw.WriteStartElement("atom", "link", "http://www.w3.org/2005/Atom");
                xw.WriteAttributeString("href", new Uri(rootUrl, Url.Action("Rss", "Feed")).ToString());
                xw.WriteAttributeString("rel", "self");
                xw.WriteAttributeString("type", "application/rss+xml");
                xw.WriteEndElement(); // atom:link
                
                foreach (var post in _repo.Posts().Take(10))
                {
                    xw.WriteStartElement("item");
                    xw.WriteElementString("title", post.Title);
                    
                    var postRelUrl = Url.Action("Post", "Blog", new { year = post.Date.Year, month = post.Date.Month, day = post.Date.Day, slug = post.Slug });
                    var postAbsUrl = new Uri(rootUrl, postRelUrl);
                    xw.WriteElementString("link", postAbsUrl.ToString());
                    xw.WriteElementString("guid", postAbsUrl.ToString());
                    xw.WriteElementString("author", $"{post.Author.Email} ({post.Author.Name})"); 
                    xw.WriteElementString("pubDate", post.Date.ToString("r"));
                    foreach (var cat in post.Categories)
                    {
                        xw.WriteElementString("category", cat.Title);
                    }
                    foreach (var tag in post.Tags)
                    {
                        xw.WriteElementString("category", tag.Title);
                    }
                    xw.WriteStartElement("content", "encoded", "http://purl.org/rss/1.0/modules/content/");
                    xw.WriteCData(await post.Content());
                    xw.WriteEndElement(); // content:encoded
                    xw.WriteEndElement(); // item
                }
                
                xw.WriteEndElement(); // channel
                xw.WriteEndElement(); // rss
            }
        }
        
        [RouteAttribute("atom")]
        public IActionResult RootAtom()
        {
            return Atom();
        }

        public IActionResult Atom()
        {
            return new TextWriterResult("application/atom+xml", AtomAsync);
        }

        async Task AtomAsync(TextWriter writer)
        {
            _telemetryClient.TrackPageView($"{Request.Path}{Request.QueryString}");

            var rootUrl = new Uri(Request.Scheme + "://" + Request.Host.ToUriComponent());

            var settings = new XmlWriterSettings()
            {
                Indent = true,
                NewLineHandling = NewLineHandling.Entitize,
            };

            using (var xw =  XmlWriter.Create(writer, settings))
            {
                var latestPost = _repo.Posts().Take(10).OrderByDescending(p => p.DateModified).First();
                
                xw.WriteStartElement(null, "feed", "http://www.w3.org/2005/Atom");
                xw.WriteElementString(null, "id", "http://www.w3.org/2005/Atom", rootUrl.ToString());
                xw.WriteElementString(null, "title", "http://www.w3.org/2005/Atom", "DevHawk");
                xw.WriteElementString(null, "updated", "http://www.w3.org/2005/Atom", latestPost.DateModified.ToString("o"));
                xw.WriteStartElement(null, "link", "http://www.w3.org/2005/Atom");
                xw.WriteAttributeString("rel", "self");
                xw.WriteAttributeString("href", new Uri(rootUrl, Url.Action("Atom", "Feed")).ToString());
                xw.WriteEndElement(); // link
                foreach (var post in _repo.Posts().Take(10))
                {
                    var postRelUrl = Url.Action("Post", "Blog", new { year = post.Date.Year, month = post.Date.Month, day = post.Date.Day, slug = post.Slug });
                    var postAbsUrl = new Uri(rootUrl, postRelUrl);

                    xw.WriteStartElement(null, "entry", "http://www.w3.org/2005/Atom");

                    xw.WriteElementString(null, "id", "http://www.w3.org/2005/Atom", postAbsUrl.ToString());
                    xw.WriteElementString(null, "title", "http://www.w3.org/2005/Atom", post.Title);
                    xw.WriteElementString(null, "updated", "http://www.w3.org/2005/Atom", post.DateModified.ToString("o"));

                    xw.WriteStartElement(null, "link", "http://www.w3.org/2005/Atom");
                    xw.WriteAttributeString("rel", "alternate");
                    xw.WriteAttributeString("href", postRelUrl.ToString());
                    xw.WriteEndElement(); // link

                    xw.WriteStartElement(null, "author", "http://www.w3.org/2005/Atom");
                    xw.WriteElementString(null, "name", "http://www.w3.org/2005/Atom", post.Author.Name);
                    xw.WriteElementString(null, "email", "http://www.w3.org/2005/Atom", post.Author.Email);
                    xw.WriteEndElement(); // link

                    foreach (var cat in post.Categories)
                    {
                        xw.WriteStartElement(null, "category", "http://www.w3.org/2005/Atom");
                        xw.WriteAttributeString("term", cat.Slug);
                        xw.WriteAttributeString("label", cat.Title);
                        xw.WriteEndElement(); // category
                    }
                    foreach (var tag in post.Tags)
                    {
                        xw.WriteStartElement(null, "category", "http://www.w3.org/2005/Atom");
                        xw.WriteAttributeString("term", tag.Slug);
                        xw.WriteAttributeString("label", tag.Title);
                        xw.WriteEndElement(); // category
                    }
                       
                    xw.WriteStartElement(null, "content", "http://www.w3.org/2005/Atom");
                    xw.WriteAttributeString("type", "html");
                    xw.WriteValue(await post.Content());
                    xw.WriteEndElement(); // content
                    xw.WriteEndElement(); // entry
                }

                xw.WriteEndElement(); // feed
            }
        }
    }
}
