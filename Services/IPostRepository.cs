using Microsoft.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hawk
{
    public interface IPostRepository
    {
        Task InitializeAsync(ILoggerFactory loggerFactory);

        IEnumerable<Post> Posts();
        IEnumerable<Tuple<Category, int>> Tags();
        IEnumerable<Tuple<Category, int>> Categories();
            
        Post PostByDasBlogEntryId(Guid entryId);
        Post PostByDasBlogTitle(string title);
        Post PostByDasBlogTitle(string title, DateTimeOffset date);
    }
}