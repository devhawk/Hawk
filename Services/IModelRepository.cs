using System;
using System.Collections.Generic;

namespace HawkProto2
{
    public interface IModelRepository
    {
        IEnumerable<Post> AllPosts();
        IEnumerable<Post> RecentPosts(int count, int skip = 0);
        Post PostByYMDSlug(int year, int month, int day, string slug);
        Post PostBySlug(string slug);
        int CountAllPosts();
        
        IEnumerable<Tuple<Category, int>> AllCategories();
        Category CategoryBySlug(string slug);
        IEnumerable<Post> PostsByCategorySlug(string categorySlug, int count, int skip = 0);
        int PostCountByCategorySlug(string categorySlug);

        IEnumerable<Tuple<Tag, int>> AllTags();
        Tag TagBySlug(string slug);
        IEnumerable<Post> PostsByTagSlug(string tagSlug, int count, int skip = 0);
        int PostCountByTagSlug(string tagSlug);

        IEnumerable<Post> PostsByYear(int year, int count, int skip);
        IEnumerable<Post> PostsByMonth(int year, int month, int count, int skip);
        IEnumerable<Post> PostsByDay(int year, int month, int day, int count, int skip);

        int PostCountByYear(int year);
        int PostCountByMonth(int year, int month);
        int PostCountByDay(int year, int month, int day);
    }
}