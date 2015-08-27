using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hawk
{
    public class Category
    {
        public string Slug { get; set; }
        public string Title { get; set; }

        public static IEnumerable<Category> ConvertCsvCatString(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Enumerable.Empty<Category>();
            }

            return text.Split(',')
                .Select(s => s.Split('|'))
                .Select((string[] a) => new Category
                {
                    Title = a[0],
                    Slug = a[1],
                });
        }
    }

    public class Author
    {
        public string Slug { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    public class CommentAuthor
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Url { get; set; }
    }
    
    public class Comment
    {
        public CommentAuthor Author { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Content { get; set; }
    }

    public class PostAuthor
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Email { get; set; }

        public static PostAuthor ConvertPostAuthor(string author)
        {
            var a = author.Split('|');
            return new PostAuthor
            {
                Name = a[0],
                Slug = a[1],
                Email = a[2],
            };
        }
    }

    public class Post
    {
        public string Slug { get; set; }
        public string Title { get; set; }
        public DateTimeOffset Date { get; set; }
        public DateTimeOffset DateModified { get; set; }

        public IList<Category> Categories { get; set; }
        public IList<Category> Tags { get; set; }
        public PostAuthor Author { get; set; }
        
        public int CommentCount { get; set; }

        public Guid? DasBlogEntryId { get; set; }
        public string DasBlogTitle { get; set; }
        public string DasBlogUniqueTitle { get; set; }

        public Func<Task<string>> Content { get; set; }
        public Func<Task<IEnumerable<Comment>>> Comments { get; set; }
    }

}