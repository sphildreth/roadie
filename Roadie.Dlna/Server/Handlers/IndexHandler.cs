using System.Linq;
using Roadie.Dlna.Utility;

namespace Roadie.Dlna.Server
{
    internal sealed class IndexHandler : IPrefixHandler
    {
        private readonly HttpServer owner;

        public string Prefix => "/";

        public IndexHandler(HttpServer owner)
        {
            this.owner = owner;
        }

        public IResponse HandleRequest(IRequest req)
        {
            var article = HtmlTools.CreateHtmlArticle("Index");
            var document = article.OwnerDocument;
            if (document == null)
            {
                throw new HttpStatusException(HttpCode.InternalError);
            }

            var list = document.EL("ul");
            var mounts = owner.MediaMounts.OrderBy(m => m.Value, NaturalStringComparer.Comparer);
            foreach (var m in mounts)
            {
                var li = document.EL("li");
                li.AppendChild(document.EL(
                  "a",
                  new AttributeCollection { { "href", m.Key } },
                  m.Value));
                list.AppendChild(li);
            }

            article.AppendChild(list);

            return new StringResponse(HttpCode.Ok, document.OuterXml);
        }
    }
}