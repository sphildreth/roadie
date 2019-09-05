using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public class Dlna : IDlna
    {
        public bool IsEnabled { get; set; }

        public int? Port { get; set; }

        public string FriendlyName { get; set; }

        public string Description { get; set; }

        public IEnumerable<string> AllowedIps { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> AllowedUserAgents { get; set; } = Enumerable.Empty<string>();

        public Dlna()
        {
            IsEnabled = true;
            FriendlyName = "Roadie Music Server";
        }
    }
}
