using System;

namespace Hawk
{
    public class Comment
    {
        public CommentAuthor Author { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Content { get; set; }
    }
}
