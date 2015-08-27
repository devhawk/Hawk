namespace Hawk.Models
{
    public class PostAuthor
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Email { get; set; }

        public static PostAuthor FromString(string author)
        {
            var a = author.Split('|');
            return new PostAuthor
            {
                Name = a[0],
                Slug = a[1],
                Email = a[2],
            };
        }
    }
}
