using System;
using System.Collections.Generic;

namespace HawkProto2
{
    class MockPostRepository : IPostRepository
    {
        public IEnumerable<Post> AllPosts() { return null; }
        public IEnumerable<Post> RecentPosts(int count, int skip = 0) { return null; }
        public Post PostByYMDSlug(int year, int month, int day, string slug) { return null; }
        public Post PostBySlug(string slug) { return null; }
        public int CountAllPosts() { return 0; }

        public IEnumerable<Tuple<Category, int>> AllCategories() { return null; }
        public Category CategoryBySlug(string slug) { return null; }
        public IEnumerable<Post> PostsByCategorySlug(string categorySlug, int count, int skip = 0) { return null; }
        public int PostCountByCategorySlug(string categorySlug) { return 0; }

        public IEnumerable<Tuple<Tag, int>> AllTags() { return null; }
        public Tag TagBySlug(string slug) { return null; }
        public IEnumerable<Post> PostsByTagSlug(string tagSlug, int count, int skip = 0) { return null; }
        public int PostCountByTagSlug(string tagSlug) { return 0; }

        public IEnumerable<Post> PostsByYear(int year, int count, int skip) { return null; }
        public IEnumerable<Post> PostsByMonth(int year, int month, int count, int skip) { return null; }
        public IEnumerable<Post> PostsByDay(int year, int month, int day, int count, int skip) { return null; }

        public int PostCountByYear(int year) { return 0; }
        public int PostCountByMonth(int year, int month) { return 0; }
        public int PostCountByDay(int year, int month, int day) { return 0; }
    }
}