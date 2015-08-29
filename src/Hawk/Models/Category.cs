using System.Collections.Generic;
using System.Linq;

namespace Hawk.Models
{
    public class Category
    {
        public string Slug { get; set; }
        public string Title { get; set; }

        public static IEnumerable<Category> FromString(string text)
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

        public static string ToString(Category cat)
        {
            return cat == null ? string.Empty : $"{cat.Title}|{cat.Slug}";
        }

        public static string ToString(IEnumerable<Category> cats)
        {
            return cats.Count() == 0 ? string.Empty : cats
                .Select(Category.ToString)
                .Aggregate((a, b) => a + "," + b);
        }
    }
}
