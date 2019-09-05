using Roadie.Dlna.Utility;
using System.Reflection;
using System.Xml;

namespace Roadie.Dlna.Server
{
    internal static class HtmlTools
    {
        public static XmlElement CreateHtmlArticle(string title)
        {
            title += " – Roadie Music Server";

            var document = new XmlDocument();
            document.AppendChild(document.CreateDocumentType(
              "html", null, null, null));

            document.AppendChild(document.EL("html"));

            var head = document.EL("head");
            document.DocumentElement?.AppendChild(head);
            head.AppendChild(document.EL("title", title));
            head.AppendChild(document.EL(
              "link",
              new AttributeCollection
              {
          {"rel", "stylesheet"},
          {"type", "text/css"},
          {"href", "/static/browse.css"}
              }));

            var body = document.EL("body");
            document.DocumentElement?.AppendChild(body);

            var article = document.EL("article");
            body.AppendChild(article);

            var header = document.EL("header");
            header.AppendChild(document.EL("h1", title));
            article.AppendChild(header);

            var footer = document.EL("footer");
            footer.AppendChild(document.EL(
              "img",
              new AttributeCollection { { "src", "/icon/small.png" } }
                                 ));
            footer.AppendChild(document.EL("h3",
                                           $"Roadie Music Server: roadie/{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}"));
            footer.AppendChild(document.EL(
              "p",
              new AttributeCollection { { "class", "desc" } },
              "A powerful API server."
                                 ));
            footer.AppendChild(document.EL(
              "a",
              new AttributeCollection
              {
                {"href", "https://github.com/sphildreth/roadie/"}
              },
              "Fork me on GitHub")
              );
            body.AppendChild(footer);
            return article;
        }
    }
}