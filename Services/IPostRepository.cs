using System;
using System.Collections.Generic;

namespace HawkProto2
{
    public interface IPostRepository
    {
        IEnumerable<Post> Posts();
        IEnumerable<Tuple<Tag, int>> Tags();
        IEnumerable<Tuple<Category, int>> Categories();
    }
}