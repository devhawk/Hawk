using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HawkProto2
{
    public class Category
    {
        public string Slug { get; set; }
        public string Title { get; set; }
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
        public int Id { get; set; }
        public CommentAuthor Author { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Content { get; set; }
    }

    public class DasBlogCompat
    {
        public Guid EntryId { get; set; }
        public string Slug { get; set; }
        public string UniqueSlug { get; set; }
    }

    public class PostAuthor
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Email { get; set; }
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

        public Func<Task<string>> Content { get; set; }
        public Func<Task<IEnumerable<Comment>>> Comments { get; set; }
    }

}