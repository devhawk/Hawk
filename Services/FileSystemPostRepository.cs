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

        public IEnumerable<Post> AllPosts() { return _posts; }
        public IEnumerable<Post> RecentPosts(int count, int skip = 0) { return null; }
        public Post PostByYMDSlug(int year, int month, int day, string slug) { return null; }
        public Post PostBySlug(string slug) { return null; }
        public int CountAllPosts() { return 0; }

        public IEnumerable<Tuple<Category, int>> AllCategories() { return null; }
        public Category CategoryBySlug(string slug) { return null; }
        public IEnumerable<Post> PostsByCategorySlug(string categorySlug, int count, int skip = 0) { return null; }
        public int PostCountByCategorySlug(string categorySlug) { return 0; }

        public IEnumerable<Tuple<Tag, int>> AllTags() { return null; }
        public Tag TagBySlug(string slug) { return null; }
        public IEnumerable<Post> PostsByTagSlug(string tagSlug, int count, int skip = 0) { return null; }
        public int PostCountByTagSlug(string tagSlug) { return 0; }

        public IEnumerable<Post> PostsByYear(int year, int count, int skip) { return null; }
        public IEnumerable<Post> PostsByMonth(int year, int month, int count, int skip) { return null; }
        public IEnumerable<Post> PostsByDay(int year, int month, int day, int count, int skip) { return null; }

        public int PostCountByYear(int year) { return 0; }
        public int PostCountByMonth(int year, int month) { return 0; }
        public int PostCountByDay(int year, int month, int day) { return 0; }
    }
}