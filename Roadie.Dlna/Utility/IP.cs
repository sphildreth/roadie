using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Roadie.Dlna.Utility
{
    public static class IP
    {
        private static bool warned;

        public static IEnumerable<IPAddress> AllIPAddresses
        {
            get
            {
                try
                {
                    return GetIPsDefault().ToArray();
                }
                catch (Exception ex)
                {
                    if (!warned)
                    {
                        Trace.WriteLine($"Failed to retrieve IP addresses the usual way, falling back to naive mode, ex [{ ex }]");
                        warned = true;
                    }
                    return GetIPsFallback();
                }
            }
        }

        public static IEnumerable<IPAddress> ExternalIPAddresses => from i in AllIPAddresses
                                                                    where !IPAddress.IsLoopback(i)
                                                                    select i;

        private static IEnumerable<IPAddress> GetIPsDefault()
        {
            var returned = false;
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                var props = adapter.GetIPProperties();
                var gateways = from ga in props.GatewayAddresses
                               where !ga.Address.Equals(IPAddress.Any)
                               select true;
                if (!gateways.Any())
                {
                    Trace.WriteLine("Skipping {props}. No gateways");
                    continue;
                }
                Trace.WriteLine($"Using {props}");
                foreach (var uni in props.UnicastAddresses)
                {
                    var address = uni.Address;
                    if (address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        Trace.WriteLine($"Skipping {address}. Not IPv4");
                        continue;
                    }
                    Trace.WriteLine($"Found {address}");
                    returned = true;
                    yield return address;
                }
            }
            if (!returned)
            {
                throw new ApplicationException("No IP");
            }
        }

        private static IEnumerable<IPAddress> GetIPsFallback()
        {
            var returned = false;
            foreach (var i in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (i.AddressFamily == AddressFamily.InterNetwork)
                {
                    Trace.WriteLine($"Found {i}");
                    returned = true;
                    yield return i;
                }
            }
            if (!returned)
            {
                throw new ApplicationException("No IP");
            }
        }
    }
}