using System.Collections.Generic;
using System.Linq;

namespace Roadie.Dlna.Server
{
    public static class Extensions
    {
        public static IEnumerable<string> GetExtensions(this DlnaMediaTypes types)
        {
            return (from i in DlnaMaps.Media2Ext
                    where types.HasFlag(i.Key)
                    select i.Value).SelectMany(i => i);
        }
    }
}