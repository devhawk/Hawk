using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublishDraft
{
    class Program
    {
        readonly static Func<object, Task<object>> _markdownItFunc = EdgeJs.Edge.Func(@"
var hljs  = require('highlight.js');

var md = require('markdown-it')({
  highlight: function (str, lang) {
    if (lang && hljs.getLanguage(lang)) {
      try {
        return hljs.highlight(lang, str).value;
      } catch (__) {}
    }

    try {
      return hljs.highlightAuto(str).value;
    } catch (__) {}

    return ''; // use external default escaping
  }
});

md.use(require('markdown-it-emoji'), {shortcuts:{}});
md.use(require('markdown-it-footnote'));
md.use(require('markdown-it-sup'));
var mdContainer = require('markdown-it-container');
md.use(mdContainer, ""image-right"");
md.use(mdContainer, ""image-left"");

return function (data, callback) {
    var renderedHtml = md.render(data);

    callback(null, renderedHtml);
}");

        public static async Task<string> MarkdownItAsync(string markdown)
        {
            return (string)await _markdownItFunc(markdown);
        }

        const string CONTENT_FOLDER = @"E:\dev\DevHawk\Content";
        const string DRAFTS_FOLDER = @"E:\dev\DevHawk\Drafts";

        static async Task MainAsync(string draftName, bool publish)
        {
            if (string.IsNullOrEmpty(draftName))
            {
                throw new ArgumentNullException(nameof(draftName));
            }

            var mdFile = Path.ChangeExtension(Path.Combine(DRAFTS_FOLDER, draftName), "md");

            if (!File.Exists(mdFile))
            {
                throw new ArgumentException(Path.GetFileName(mdFile) + " doesn't exist");
            }

            var markdown = File.ReadAllText(mdFile);
            var html = await MarkdownItAsync(markdown);

            if (!publish)
            {
                var htmlFile = Path.ChangeExtension(mdFile, "html");
                Console.WriteLine($"Savinging rendered markdown to {htmlFile}");

                File.WriteAllText(htmlFile, html);
                return;
            }

            var jsonFile = Path.ChangeExtension(mdFile, "json");
            if (!File.Exists(jsonFile))
            {
                throw new ArgumentException(Path.GetFileName(jsonFile) + " doesn't exist");
            }

            var now = DateTimeOffset.Now;
            dynamic hawkPost = JObject.Parse(File.ReadAllText(jsonFile));
            hawkPost.date = now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'sszzz");
            hawkPost.modified = hawkPost.date;
            hawkPost.author = "DevHawk|devhawk|harry@devhawk.net";
            hawkPost["comment-count"] = 0;

            var folderName = $"{now.ToString("yyyyMMdd")}-{hawkPost.slug}";

            var postFolder = Path.Combine(CONTENT_FOLDER, folderName);
            if (!Directory.Exists(postFolder))
            {
                Console.WriteLine($"Creating new post folder {folderName}");
                Directory.CreateDirectory(postFolder);
            }
            Console.WriteLine($"Publishing to folder {folderName}");

            File.WriteAllText(Path.Combine(postFolder, "hawk-post.json"), hawkPost.ToString());
            File.WriteAllText(Path.Combine(postFolder, "content.md"), markdown);
            File.WriteAllText(Path.Combine(postFolder, "rendered-content.html"), html);
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                // if no args are passed, rendered each of the markdown files in the drafts folder to html in the draft folder
                var files = Directory.EnumerateFiles(DRAFTS_FOLDER, "*.md").Select(file => Path.GetFileName(file));

                foreach (var file in files)
                {
                    Console.WriteLine(file);
                    MainAsync(file, false).Wait();
                }
                return;
            }

            if (string.Compare(args[0], "-list", StringComparison.OrdinalIgnoreCase) == 0)
            {
                // list all the markdown files in the draft folder
                var files = Directory.EnumerateFiles(DRAFTS_FOLDER, "*.md").Select(file => Path.GetFileName(file));
                foreach (var file in files)
                {
                    Console.WriteLine(file);
                }
                return;
            }
            
            // publish the specified draft
            var filename = args[0];
            var publish = (args.Length > 1 && (string.Compare(args[1], "publish", StringComparison.OrdinalIgnoreCase) == 0));

            MainAsync(filename, publish).Wait();
        }
    }
}

