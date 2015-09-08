Hawk
====

Hawk is the blog engine that powers [DevHawk](http://devhawk.net). 
Yes, I realize that's very early 00's of me, but as I said in my
[blog relaunch post](http://devhawk.net/blog/2015/8/31/go-ahead-call-it-a-comeback), I'm 
tired of running other people's software on my site. Hawk is my personal web sandbox, but
you're welcome to play in it too. Consider this a small contribution to
the [open web](http://scripting.com/stories/2011/01/04/whatIMeanByTheOpenWeb.html).

Relevant Notes
--------------

 * Written in C# and [ASP.NET 5](https://github.com/aspnet/home)
 * Uses Azure Table Storage for blog metadata and legacy comments and Azure Blob Storage for blog post content (original markdown and rendered HTML)
   * Also includes file system based post repository for development purposes
 * Uses [Markdown-it](https://github.com/markdown-it/markdown-it) with 
 several custom syntax extensions for markdown processing
 * Uses [Edge.js](http://tjanczuk.github.io/edge/) to enable calling Markdown-it from C# 
 ([more details](http://devhawk.net/blog/2015/9/2/the-brilliant-magic-of-edgejs))
 * Uses [Bootstrap](http://getbootstrap.com/) as the client-side framework with 
 a custom theme of my own design.