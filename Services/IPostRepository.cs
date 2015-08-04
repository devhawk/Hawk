using System;
using System.Collections.Generic;

namespace Hawk
{
    public interface IPostRepository
    {
        IEnumerable<Post> Posts();
        IEnumerable<Tuple<Category, int>> Tags();
        IEnumerable<Tuple<Category, int>> Categories();
            
        Post PostByDasBlogEntryId(Guid entryId);
        Post PostByDasBlogTitle(string title);
        Post PostByDasBlogTitle(string title, DateTimeOffset date);
    }
}