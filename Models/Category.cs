using System.Collections.Generic;
using System.Linq;

namespace Hawk.Models
{
    public class Category
    {
        public string Slug { get; set; }
        public string Title { get; set; }

        public static IEnumerable<Category> FromCsvCatString(string text)
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
    }
}
