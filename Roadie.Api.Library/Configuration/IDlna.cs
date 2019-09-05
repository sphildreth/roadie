using System.Collections.Generic;

namespace Roadie.Library.Configuration
{
    public interface IDlna
    {
        bool IsEnabled { get; set; }
        string Description { get; set; }
        string FriendlyName { get; set; }
        int? Port { get; set; }
        IEnumerable<string> AllowedIps { get; set; }
        IEnumerable<string> AllowedUserAgents { get; set; }
    }
}