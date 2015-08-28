using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Hawk.Models
{
    public class Comment
    {
        public CommentAuthor Author { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Content { get; set; }

        public string UniqueKey
        {
            get
            {
                return Date.ToString("yyyyMMdd-HHmmss");
            }
        }


        public static Comment FromDte(DynamicTableEntity dte)
        {
            return new Comment
            {
                Content = dte.Properties["Content"].StringValue,
                Date = dte.Properties["Date"].DateTimeOffsetValue.Value,
                Author = new CommentAuthor
                {
                    Name = dte.Properties["AuthorName"].StringValue,
                    Email = dte.Properties["AuthorEmail"].StringValue,
                    Url = dte.Properties["AuthorUrl"].StringValue,
                },
            };
        }

        public static DynamicTableEntity ToDte(Comment comment, string postKey)
        {
            var dte = new DynamicTableEntity(postKey, comment.UniqueKey);

            dte.Properties.Add("Date", EntityProperty.GeneratePropertyForDateTimeOffset(comment.Date));
            dte.Properties.Add("Content", EntityProperty.GeneratePropertyForString(comment.Content));
            dte.Properties.Add("AuthorName", EntityProperty.GeneratePropertyForString(comment.Author.Name));
            dte.Properties.Add("AuthorEmail", EntityProperty.GeneratePropertyForString(comment.Author.Email));
            dte.Properties.Add("AuthorUrl", EntityProperty.GeneratePropertyForString(comment.Author.Url));

            return dte;
        }
    }
}
