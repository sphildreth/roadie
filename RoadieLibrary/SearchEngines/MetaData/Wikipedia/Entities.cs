using System.Collections.Generic;

namespace Roadie.Library.SearchEngines.MetaData.Wikipedia
{
    public class api
    {
        public query query { get; set; }
    }

    public class query
    {
        public List<page> pages { get; set; }
    }

    public class page
    {
        public string extract { get; set; }
    }
}