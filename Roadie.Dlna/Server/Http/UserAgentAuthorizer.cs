using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace Roadie.Dlna.Server
{
    public sealed class UserAgentAuthorizer : IHttpAuthorizationMethod
    {
        private readonly Dictionary<string, object> userAgents = new Dictionary<string, object>();

        public UserAgentAuthorizer(IEnumerable<string> userAgents)
        {
            if (userAgents == null)
            {
                throw new ArgumentNullException(nameof(userAgents));
            }
            foreach (var u in userAgents)
            {
                if (string.IsNullOrEmpty(u))
                {
                    throw new FormatException("Invalid User-Agent supplied");
                }
                this.userAgents.Add(u, null);
            }
        }

        public bool Authorize(IHeaders headers, IPEndPoint endPoint)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }
            string ua;
            if (!headers.TryGetValue("User-Agent", out ua))
            {
                return false;
            }
            if (string.IsNullOrEmpty(ua))
            {
                return false;
            }
            var rv = userAgents.ContainsKey(ua);
            Trace.WriteLine(!rv ? $"Rejecting {ua}. Not in User-Agent whitelist" : $"Accepted {ua} via User-Agent whitelist");
            return rv;
        }
    }
}