using Microsoft.Extensions.Logging;
using Roadie.Dlna.Server.Ssdp;
using Roadie.Dlna.Utility;
using Roadie.Library.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Timers;

namespace Roadie.Dlna.Server
{
    public sealed class HttpServer : IDisposable
    {
        public static readonly string Signature = GenerateServerSignature();

        private readonly ConcurrentDictionary<HttpClient, DateTime> clients = new ConcurrentDictionary<HttpClient, DateTime>();

        private readonly ConcurrentDictionary<Guid, List<Guid>> devicesForServers = new ConcurrentDictionary<Guid, List<Guid>>();

        private readonly TcpListener listener;

        private readonly ConcurrentDictionary<string, IPrefixHandler> prefixes = new ConcurrentDictionary<string, IPrefixHandler>();

        private readonly ConcurrentDictionary<Guid, MediaMount> servers = new ConcurrentDictionary<Guid, MediaMount>();

        private readonly SsdpHandler ssdpServer;

        private readonly Timer timeouter = new Timer(10 * 1000);

        public ILogger Logger { get; }

        public Dictionary<string, string> MediaMounts
        {
            get
            {
                var rv = new Dictionary<string, string>();
                foreach (var m in servers)
                {
                    rv[m.Value.Prefix] = m.Value.FriendlyName;
                }
                return rv;
            }
        }

        public int RealPort { get; }

        public HttpServer(ILogger logger, int port)
        {
            Logger = logger;

            prefixes.TryAdd(
              "/favicon.ico",
              new StaticHandler(
                new ResourceResponse(HttpCode.Ok, "image/icon", "favicon.ico"))
              );
            prefixes.TryAdd(
              "/static/browse.css",
              new StaticHandler(
                new ResourceResponse(HttpCode.Ok, "text/css", "browse.css"))
              );
            RegisterHandler(new IconHandler());

            listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            listener.Server.Ttl = 32;
            listener.Start();

            RealPort = ((IPEndPoint)listener.LocalEndpoint).Port;

            Logger.LogInformation($"Running DLNA HTTP Server: {Signature} on port {RealPort}");
            ssdpServer = new SsdpHandler(logger);

            timeouter.Elapsed += TimeouterCallback;
            timeouter.Enabled = true;

            Accept();
        }

        public event EventHandler<HttpAuthorizationEventArgs> OnAuthorizeClient;

        public void Dispose()
        {
            Logger.LogTrace("Disposing HTTP");
            timeouter.Enabled = false;
            foreach (var s in servers.Values.ToList())
            {
                UnregisterMediaServer(s);
            }
            ssdpServer.Dispose();
            timeouter.Dispose();
            listener.Stop();
            foreach (var c in clients.ToList())
            {
                c.Key.Dispose();
            }
            clients.Clear();
        }

        public void RegisterMediaServer(IRoadieSettings configuration, ILogger logger, IMediaServer server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }
            var guid = server.UUID;
            if (servers.ContainsKey(guid))
            {
                throw new ArgumentException("Attempting to register more than once");
            }

            var end = (IPEndPoint)listener.LocalEndpoint;
            var mount = new MediaMount(configuration, logger, server);
            servers[guid] = mount;
            RegisterHandler(mount);

            foreach (var address in IP.ExternalIPAddresses)
            {
                Logger.LogTrace($"Registering device for {address}");
                var deviceGuid = Guid.NewGuid();
                var list = devicesForServers.GetOrAdd(guid, new List<Guid>());
                lock (list)
                {
                    list.Add(deviceGuid);
                }
                mount.AddDeviceGuid(deviceGuid, address);
                var uri = new Uri($"http://{address}:{end.Port}{mount.DescriptorURI}");
                lock (list)
                {
                    ssdpServer.RegisterNotification(deviceGuid, uri, address);
                }
                Logger.LogTrace($"New mount at: {uri}");
            }
        }

        public void UnregisterMediaServer(IMediaServer server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }
            MediaMount mount;
            if (!servers.TryGetValue(server.UUID, out mount))
            {
                return;
            }

            List<Guid> list;
            if (devicesForServers.TryGetValue(server.UUID, out list))
            {
                lock (list)
                {
                    foreach (var deviceGuid in list)
                    {
                        ssdpServer.UnregisterNotification(deviceGuid);
                    }
                }
                devicesForServers.TryRemove(server.UUID, out list);
            }

            UnregisterHandler(mount);

            MediaMount ignored;
            if (servers.TryRemove(server.UUID, out ignored))
            {
                Logger.LogTrace($"Unregistered Media Server {server.UUID}");
            }
        }

        internal bool AuthorizeClient(HttpClient client)
        {
            if (OnAuthorizeClient == null)
            {
                return true;
            }
            if (IPAddress.IsLoopback(client.RemoteEndpoint.Address))
            {
                return true;
            }
            var e = new HttpAuthorizationEventArgs(client.Headers, client.RemoteEndpoint);
            OnAuthorizeClient(this, e);
            return !e.Cancel;
        }

        internal IPrefixHandler FindHandler(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (prefix == "/")
            {
                return new IndexHandler(this);
            }

            return (from s in prefixes.Keys
                    where prefix.StartsWith(s, StringComparison.Ordinal)
                    select prefixes[s]).FirstOrDefault();
        }

        internal void RegisterHandler(IPrefixHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            var prefix = handler.Prefix;
            if (!prefix.StartsWith("/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Invalid prefix; must start with /");
            }
            if (!prefix.EndsWith("/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Invalid prefix; must end with /");
            }
            if (FindHandler(prefix) != null)
            {
                throw new ArgumentException("Invalid prefix; already taken");
            }
            if (!prefixes.TryAdd(prefix, handler))
            {
                throw new ArgumentException("Invalid preifx; already taken");
            }
            Logger.LogTrace($"Registered Handler for {prefix}");
        }

        internal void RemoveClient(HttpClient client)
        {
            DateTime ignored;
            clients.TryRemove(client, out ignored);
        }

        internal void UnregisterHandler(IPrefixHandler handler)
        {
            IPrefixHandler ignored;
            if (prefixes.TryRemove(handler.Prefix, out ignored))
            {
                Logger.LogTrace($"Unregistered Handler for {handler.Prefix}");
            }
        }

        private static string GenerateServerSignature()
        {
            var os = Environment.OSVersion;
            var pstring = os.Platform.ToString();
            switch (os.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                    pstring = "WIN";
                    break;

                default:
                    try
                    {
                        pstring = Formatting.GetSystemName();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Failed to get uname Ex [{ ex }]", "Warning");
                    }
                    break;
            }
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var bitness = IntPtr.Size * 8;
            return
              $"{pstring}{bitness}/{os.Version.Major}.{os.Version.Minor} UPnP/1.0 DLNADOC/1.5 roadie/{version.Major}.{version.Minor}";
        }

        private void Accept()
        {
            try
            {
                if (!listener.Server.IsBound)
                {
                    return;
                }
                listener.BeginAcceptTcpClient(AcceptCallback, null);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"Failed to accept [{ ex }]");
            }
        }

        private void AcceptCallback(IAsyncResult result)
        {
            try
            {
                var tcpclient = listener.EndAcceptTcpClient(result);
                var client = new HttpClient(this, tcpclient);
                try
                {
                    clients.AddOrUpdate(client, DateTime.Now, (k, v) => DateTime.Now);
                    Logger.LogTrace($"Accepted client {client}");
                    client.Start();
                }
                catch (Exception)
                {
                    client.Dispose();
                    throw;
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"Failed to accept a client Ex [{ ex }]");
            }
            finally
            {
                Accept();
            }
        }

        private void TimeouterCallback(object sender, ElapsedEventArgs e)
        {
            foreach (var c in clients.ToList())
            {
                if (c.Key.IsATimeout)
                {
                    Logger.LogTrace($"Collected timeout client {c}");
                    c.Key.Close();
                }
            }
        }
    }
}