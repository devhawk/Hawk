using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hawk.Models;

namespace Hawk.Services
{
    static class FileSystemRepo
    {
        public const string ITEM_JSON = "hawk-post.json";
        public const string COMMENTS_JSON = "hawk-comments.json";
        public const string RENDERED_CONTENT_FILENAME = "rendered-content.html";
        public const string CONTENT_FILENAME = "content.md";
        public static readonly string[] IMG_EXTENSIONS = { ".png", ".gif", ".jpg" };

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
