using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Roadie.Dlna.Server
{
    public sealed class IPAddressAuthorizer : IHttpAuthorizationMethod
    {
        private readonly Dictionary<IPAddress, object> ips =
          new Dictionary<IPAddress, object>();

        public IPAddressAuthorizer(IEnumerable<IPAddress> addresses)
        {
            if (addresses == null)
            {
                throw new ArgumentNullException(nameof(addresses));
            }
            foreach (var ip in addresses)
            {
                ips.Add(ip, null);
            }
        }

        public IPAddressAuthorizer(IEnumerable<string> addresses)
          : this(from a in addresses select IPAddress.Parse(a))
        {
        }

        public bool Authorize(IHeaders headers, IPEndPoint endPoint)
        {
            var addr = endPoint?.Address;
            if (addr == null)
            {
                return false;
            }
            var rv = ips.ContainsKey(addr);
            Trace.WriteLine(!rv ? $"Rejecting {addr}. Not in IP whitelist" : $"Accepted {addr} via IP whitelist");
            return rv;
        }
    }
}