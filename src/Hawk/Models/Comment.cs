using System;

namespace Hawk.Models
{
    public class Comment
    {
        public CommentAuthor Author { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Content { get; set; }
    }
}
