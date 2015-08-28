using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Hawk.Models
{
    public class Comment
    {
        public CommentAuthor Author { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Content { get; set; }

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

    }
}
