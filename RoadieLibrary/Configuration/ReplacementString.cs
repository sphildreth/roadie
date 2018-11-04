using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public class ReplacementString
    {
        public int Order { get; set; }
        public string Key { get; set; }
        public string ReplaceWith { get; set; }
    }
}
