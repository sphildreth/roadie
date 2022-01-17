﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Roadie.Dlna.Server.Ssdp
{
    internal sealed class SsdpHandler : IDisposable
    {
        internal static readonly IPEndPoint BroadEndp =
          new IPEndPoint(IPAddress.Parse("255.255.255.255"), SSDP_PORT);

        private const int DATAGRAMS_PER_MESSAGE = 3;

        private const string SSDP_ADDR = "239.255.255.250";

        private const int SSDP_PORT = 1900;

        private static readonly Random random = new Random();

        private static readonly IPEndPoint ssdpEndp = new IPEndPoint(IPAddress.Parse(SSDP_ADDR), SSDP_PORT);

        private static readonly IPAddress ssdpIP = IPAddress.Parse(SSDP_ADDR);

        private readonly UdpClient client = new UdpClient();

        private readonly AutoResetEvent datagramPosted = new AutoResetEvent(false);

        private readonly Dictionary<Guid, List<UpnpDevice>> devices = new Dictionary<Guid, List<UpnpDevice>>();

        private readonly ConcurrentQueue<Datagram> messageQueue = new ConcurrentQueue<Datagram>();

        private readonly Timer notificationTimer = new Timer(60000);

        private readonly Timer queueTimer = new Timer(1000);

        private bool running = true;

        private ILogger Logger { get; }

        private UpnpDevice[] Devices
        {
            get
            {
                UpnpDevice[] devs;
                lock (devices)
                {
                    devs = devices.Values.SelectMany(i => i).ToArray();
                }
                return devs;
            }
        }

        public SsdpHandler(ILogger logger)
        {
            Logger = logger;

            notificationTimer.Elapsed += Tick;
            notificationTimer.Enabled = true;

            queueTimer.Elapsed += ProcessQueue;

            client.Client.SetSocketOption(
              SocketOptionLevel.Socket,
              SocketOptionName.ReuseAddress,
              true
              );
            client.ExclusiveAddressUse = false;
            client.Client.Bind(new IPEndPoint(IPAddress.Any, SSDP_PORT));
            client.JoinMulticastGroup(ssdpIP, 10);
            Logger.LogTrace("SSDP service started");
            Receive();
        }

        public void Dispose()
        {
            Logger.LogTrace("Disposing SSDP");
            running = false;
            while (messageQueue.Count != 0)
            {
                datagramPosted.WaitOne();
            }

            client.DropMulticastGroup(ssdpIP);

            notificationTimer.Enabled = false;
            queueTimer.Enabled = false;
            notificationTimer.Dispose();
            queueTimer.Dispose();
            datagramPosted.Dispose();
        }

        internal void NotifyAll()
        {
            Logger.LogTrace("NotifyAll");
            foreach (var d in Devices)
            {
                NotifyDevice(d, "alive", false);
            }
        }

        internal void NotifyDevice(UpnpDevice dev, string type, bool sticky)
        {
            Logger.LogTrace("NotifyDevice");
            var headers = new RawHeaders
      {
        {"HOST", "239.255.255.250:1900"},
        {"CACHE-CONTROL", "max-age = 600"},
        {"LOCATION", dev.Descriptor.ToString()},
        {"SERVER", HttpServer.Signature},
        {"NTS", "ssdp:" + type},
        {"NT", dev.Type},
        {"USN", dev.USN}
      };

            SendDatagram(
              ssdpEndp,
              dev.Address,
              $"NOTIFY * HTTP/1.1\r\n{headers.HeaderBlock}\r\n",
              sticky
              );
            // Some buggy network equipment will swallow multicast packets, so lets
            // cheat, increase the odds, by sending to broadcast.
            SendDatagram(
              BroadEndp,
              dev.Address,
              $"NOTIFY * HTTP/1.1\r\n{headers.HeaderBlock}\r\n",
              sticky
              );
            Logger.LogTrace($"{dev.USN} said {type}");
        }

        internal void RegisterNotification(Guid uuid, Uri descriptor,
          IPAddress address)
        {
            List<UpnpDevice> list;
            lock (devices)
            {
                if (!devices.TryGetValue(uuid, out list))
                {
                    devices.Add(uuid, list = new List<UpnpDevice>());
                }
            }
            list.AddRange(new[]
            {
        "upnp:rootdevice", "urn:schemas-upnp-org:device:MediaServer:1",
        "urn:schemas-upnp-org:service:ContentDirectory:1", "urn:schemas-upnp-org:service:ConnectionManager:1",
        "urn:schemas-upnp-org:service:X_MS_MediaReceiverRegistrar:1", "uuid:" + uuid
      }.Select(t => new UpnpDevice(uuid, t, descriptor, address)));

            NotifyAll();
            Logger.LogTrace($"Registered mount {uuid}, {address}");
        }

        internal void RespondToSearch(IPEndPoint endpoint, string req)
        {
            if (req == "ssdp:all")
            {
                req = null;
            }

            Logger.LogTrace("RespondToSearch {endpoint} {req}");
            foreach (var d in Devices)
            {
                if (!string.IsNullOrEmpty(req) && req != d.Type)
                {
                    continue;
                }
                SendSearchResponse(endpoint, d);
            }
        }

        internal void UnregisterNotification(Guid uuid)
        {
            List<UpnpDevice> dl;
            lock (devices)
            {
                if (!devices.TryGetValue(uuid, out dl))
                {
                    return;
                }
                devices.Remove(uuid);
            }
            foreach (var d in dl)
            {
                NotifyDevice(d, "byebye", true);
            }
            Logger.LogTrace("Unregistered mount {uuid}");
        }

        private void ProcessQueue(object sender, ElapsedEventArgs e)
        {
            while (messageQueue.Count != 0)
            {
                Datagram msg;
                if (!messageQueue.TryPeek(out msg))
                {
                    continue;
                }
                if (msg != null && (running || msg.Sticky))
                {
                    msg.Send();
                    if (msg.SendCount > DATAGRAMS_PER_MESSAGE)
                    {
                        messageQueue.TryDequeue(out msg);
                    }
                    break;
                }
                messageQueue.TryDequeue(out msg);
            }
            datagramPosted.Set();
            queueTimer.Enabled = messageQueue.Count != 0;
            queueTimer.Interval = random.Next(25, running ? 75 : 50);
        }

        private void Receive()
        {
            try
            {
                client.BeginReceive(ReceiveCallback, null);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                var endpoint = new IPEndPoint(IPAddress.None, SSDP_PORT);
                var received = client.EndReceive(result, ref endpoint);
                if (received == null)
                {
                    throw new IOException("Didn't receive anything");
                }
                if (received.Length == 0)
                {
                    throw new IOException("Didn't receive any bytes");
                }

                //Logger.LogTrace($"{endpoint} - SSDP Received a datagram");

                using (var reader = new StreamReader(new MemoryStream(received), Encoding.ASCII))
                {
                    var proto = reader.ReadLine();
                    if (proto == null)
                    {
                        throw new IOException("Couldn't read protocol line");
                    }
                    proto = proto.Trim();
                    if (string.IsNullOrEmpty(proto))
                    {
                        throw new IOException("Invalid protocol line");
                    }
                    var method = proto.Split(new[] { ' ' }, 2)[0];
                    var headers = new Headers();
                    for (var line = reader.ReadLine();
                      line != null;
                      line = reader.ReadLine())
                    {
                        line = line.Trim();
                        if (string.IsNullOrEmpty(line))
                        {
                            break;
                        }
                        var parts = line.Split(new[] { ':' }, 2);
                        headers[parts[0]] = parts[1].Trim();
                    }
              //      Logger.LogTrace($"{endpoint} - Datagram method: {method}");
                    if (method == "M-SEARCH")
                    {
                        RespondToSearch(endpoint, headers["st"]);
                    }
                }
            }
            catch (IOException ex)
            {
                Logger.LogTrace($"Failed to read SSDP message Ex [{ ex }]");
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"Failed to read SSDP message Ex [{ ex }]");
            }
            Receive();
        }

        private void SendDatagram(IPEndPoint endpoint, IPAddress address,
          string message, bool sticky)
        {
            if (!running)
            {
                return;
            }
            var dgram = new Datagram(endpoint, address, message, sticky);
            if (messageQueue.Count == 0)
            {
                dgram.Send();
            }
            messageQueue.Enqueue(dgram);
            queueTimer.Enabled = true;
        }

        private void SendSearchResponse(IPEndPoint endpoint, UpnpDevice dev)
        {
            var headers = new RawHeaders
      {
        {"CACHE-CONTROL", "max-age = 600"},
        {"DATE", DateTime.Now.ToString("R")},
        {"EXT", string.Empty},
        {"LOCATION", dev.Descriptor.ToString()},
        {"SERVER", HttpServer.Signature},
        {"ST", dev.Type},
        {"USN", dev.USN}
      };

            SendDatagram(
              endpoint,
              dev.Address,
              $"HTTP/1.1 200 OK\r\n{headers.HeaderBlock}\r\n",
              false
              );
            Logger.LogTrace($"{dev.Address}, {endpoint} - Responded to a {dev.Type} request");
        }

        private void Tick(object sender, ElapsedEventArgs e)
        {
            Logger.LogTrace("Sending SSDP notifications!");
            notificationTimer.Interval = random.Next(60000, 120000);
            NotifyAll();
        }
    }
}