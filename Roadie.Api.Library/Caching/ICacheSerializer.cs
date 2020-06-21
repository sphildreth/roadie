using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Caching
{
    public interface ICacheSerializer
    {
        string Serialize(object o);
        TOut Deserialize<TOut>(string s);
    }
}
