using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace HawkProto2
{
    public class Category
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("description ")]
        public string Description { get; set; }
        [JsonProperty("parent")]
        public int Parent { get; set; }
        [JsonProperty("post_count")]
        public int PostCount { get; set; }
    }

    public class Tag
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("description ")]
        public string Description { get; set; }
        [JsonProperty("post_count")]
        public int PostCount { get; set; }
    }

    public class Author
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("first_name")]
        public string FirstName { get; set; }
        [JsonProperty("last_name")]
        public string LastName { get; set; }
        [JsonProperty("nickname")]
        public string Nickname { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class Comment
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("author")]
        public Author Author { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("parent")]
        public int Parent { get; set; }
    }

    public class CustomFields
    {
        [JsonProperty("dasblog_entryid")]
        public List<string> DasBlogEntryId { get; set; }
        [JsonProperty("dasblog_compressedtitle")]
        public List<string> DasBlogCompressedTitle { get; set; }
        [JsonProperty("dasblog_compressedtitleunique")]
        public List<string> DasBlogCompressedTitleUnique { get; set; }
        [JsonProperty("enclosure")]
        public List<string> Enclosure { get; set; }
        [JsonProperty("layout_key")]
        public List<string> LayoutKey { get; set; }
        [JsonProperty("post_slider_check_key")]
        public List<string> PostSliderCheckKey { get; set; }
    }

    public class Image
    {
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("width")]
        public int Width { get; set; }
        [JsonProperty("height")]
        public int Height { get; set; }
    }

    public class Images
    {
        [JsonProperty("full")]
        public Image Full { get; set; }
        [JsonProperty("thumbnail")]
        public Image Thumbnail { get; set; }
        [JsonProperty("medium")]
        public Image Medium { get; set; }
        [JsonProperty("large")]
        public Image Large { get; set; }
        [JsonProperty("tc-thumb")]
        public Image TcThumb { get; set; }
        [JsonProperty("slider-full")]
        public Image SliderFull { get; set; }
        [JsonProperty("slider")]
        public Image Slider { get; set; }
    }

    public class Attachment
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("description ")]
        public string Description { get; set; }
        [JsonProperty("caption")]
        public string Caption { get; set; }
        [JsonProperty("parent")]
        public int Parent { get; set; }
        [JsonProperty("mime_type")]
        public string MimeType { get; set; }
        [JsonProperty("images")]
        public Images Images { get; set; }
    }

    public class Post
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("title_plain")]
        public string TitlePlain { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("excerpt")]
        public string Excerpt { get; set; }
        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }
        [JsonProperty("modified")]
        public DateTimeOffset Modified { get; set; }
        [JsonProperty("categories")]
        public List<Category> Categories { get; set; }
        [JsonProperty("tags")]
        public List<Tag> Tags { get; set; }
        [JsonProperty("author")]
        public Author Author { get; set; }
        [JsonProperty("comments")]
        public List<Comment> Comments { get; set; }
        [JsonProperty("attachements")]
        public List<Attachment> Attachments { get; set; }
        [JsonProperty("comment_count")]
        public int CommentCount { get; set; }
        [JsonProperty("comment_status")]
        public string CommentStatus { get; set; }
        [JsonProperty("custom_fields")]
        public CustomFields CustomFields { get; set; }

        public Lazy<string> RenderedContent { get; set; }
    }

    class JsonItem
    {
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("post")]
        public Post Post { get; set; }
        [JsonProperty("previous_url")]
        public string PreviousUrl { get; set; }
        [JsonProperty("next_url")]
        public string NextUrl { get; set; }
    }
}