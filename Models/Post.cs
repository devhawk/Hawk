using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hawk
{
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