using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Hawk.Models;

namespace Hawk.Services
{
    static class FileSystemRepo
    {
        public const string ITEM_JSON = "hawk-post.json";
        public const string COMMENTS_JSON = "hawk-comments.json";
        public const string ITEM_CONTENT = "rendered-content.html";

        public static IEnumerable<string> EnumeratePostDirectories(string path)
        {
            return Directory
                .EnumerateDirectories(path)
                .Where(dir => File.Exists(Path.Combine(dir, ITEM_JSON)));
        }

        public static IEnumerable<Post> EnumeratePosts(string path)
        {
            return EnumeratePostDirectories(path)
                .Select(dir => Post.FromDirectory(dir));
        }
    }
}
