using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HawkProto2
{
    class FileSystemPostRepository : IPostRepository
    {
        const string PATH = @"E:\dev\DevHawk\Content\Posts";
        const string ITEM_JSON = "json-item.json";
        const string ITEM_CONTENT = "rendered-content.html";
        
        static List<Post> _posts = null;

        static FileSystemPostRepository()
        {
            var tempPosts = new List<Post>();
            var dirs = Directory.EnumerateDirectories(PATH);
            foreach (var dir in dirs)
            {
                var jsonItemText = File.ReadAllText(Path.Combine(dir, ITEM_JSON));
                var jsonItem = JsonConvert.DeserializeObject<JsonItem>(jsonItemText);

                // temporary until I fix the content in the json files
                jsonItem.Post.Title = System.Net.WebUtility.HtmlDecode(jsonItem.Post.Title);

                // lazily load content since that will (likely) be stored seperately from metadata
                jsonItem.Post.RenderedContent = new Lazy<string>(() => File.ReadAllText(Path.Combine(dir, ITEM_CONTENT)));
                tempPosts .Add(jsonItem.Post);
            }

            _posts = tempPosts.OrderByDescending(p => p.Date).ToList();
        }

        public IEnumerable<Post> Posts() { return _posts; }
    }
}