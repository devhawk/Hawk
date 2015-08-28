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
        public static IEnumerable<Post> LoadPostsFromFileSystem(string path)
        {
            const string ITEM_JSON = "hawk-post.json";
            const string COMMENTS_JSON = "hawk-comments.json";
            const string ITEM_CONTENT = "rendered-content.html";

            var fsPosts = Directory
                .EnumerateDirectories(path)
                .Where(dir => File.Exists(Path.Combine(dir, ITEM_JSON)))
                .Select(dir => new
                {
                    Directory = dir,
                    Post = JObject.Parse(File.ReadAllText(Path.Combine(dir, ITEM_JSON))),
                });

            foreach (var fsPost in fsPosts)
            {
                yield return new Post()
                {
                    Slug = (string)fsPost.Post["slug"],
                    Title = System.Net.WebUtility.HtmlDecode((string)fsPost.Post["title"]),
                    Date = DateTimeOffset.Parse((string)fsPost.Post["date"]),
                    DateModified = DateTimeOffset.Parse((string)fsPost.Post["modified"]),
                    Categories = Category.FromCsvCatString((string)fsPost.Post["csv-category-slugs"]).ToList(),
                    Tags = Category.FromCsvCatString((string)fsPost.Post["csv-tag-slugs"]).ToList(),
                    Author = PostAuthor.FromString((string)fsPost.Post["author"]),
                    CommentCount = int.Parse((string)fsPost.Post["comment-count"]),

                    DasBlogEntryId = fsPost.Post["dasblog-entry-id"] != null ? Guid.Parse((string)fsPost.Post["dasblog-entry-id"]) : (Guid?)null,
                    DasBlogTitle = (string)fsPost.Post["dasblog-title"] ?? null,
                    DasBlogUniqueTitle = (string)fsPost.Post["dasblog-unique-title"] ?? null,

                    Content = () => Task.Run(() => File.ReadAllText(Path.Combine(fsPost.Directory, ITEM_CONTENT))),
                    Comments = () => Task.Run(() => JArray
                        .Parse(File.ReadAllText(Path.Combine(fsPost.Directory, COMMENTS_JSON)))
                        .Select(fsc => new Comment
                        {
                            Content = (string)fsc["content"],
                            Date = DateTimeOffset.Parse((string)fsc["date"]),
                            Author = new CommentAuthor
                            {
                                Name = (string)fsc["author-name"],
                                Email = (string)fsc["author-email"],
                                Url = (string)fsc["author-url"],
                            }
                        })),
                };
            }
        }
    }
}
