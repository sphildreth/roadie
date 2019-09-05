using Microsoft.Extensions.Logging;
using Roadie.Dlna.Server.Metadata;
using Roadie.Dlna.Utility;
using Roadie.Library.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Roadie.Dlna.Server
{
    internal sealed partial class MediaMount : IMediaServer, IPrefixHandler
    {
        private static uint mount;

        private readonly Dictionary<IPAddress, Guid> guidsForAddresses = new Dictionary<IPAddress, Guid>();

        private readonly IMediaServer server;

        private uint systemID = 1;

        public IHttpAuthorizationMethod Authorizer => server.Authorizer;
        private IRoadieSettings Configuration { get; }
        private ILogger Logger { get; }
        public string DescriptorURI => $"{Prefix}description.xml";

        public string FriendlyName => server.FriendlyName;

        public string Prefix { get; }

        public Guid UUID => server.UUID;

        public MediaMount(IRoadieSettings configuration, ILogger logger, IMediaServer aServer)
        {
            Configuration = configuration;
            Logger = logger;
            server = aServer;
            Prefix = $"/mm-{++mount}/";
            var vms = server as IVolatileMediaServer;
            if (vms != null)
            {
                vms.Changed += ChangedServer;
            }
        }

        public void AddDeviceGuid(Guid guid, IPAddress address)
        {
            guidsForAddresses.Add(address, guid);
        }

        public IMediaItem GetItem(string id, bool isFileRequest)
        {
            return server.GetItem(id, isFileRequest);
        }

        public IResponse HandleRequest(IRequest request)
        {
            if (Authorizer != null &&
                !IPAddress.IsLoopback(request.RemoteEndpoint.Address) &&
                !Authorizer.Authorize(
                  request.Headers,
                  request.RemoteEndpoint
                  ))
            {
                throw new HttpStatusException(HttpCode.Denied);
            }

            var path = request.Path.Substring(Prefix.Length);
            if (path == "description.xml")
            {
                return new StringResponse(
                  HttpCode.Ok,
                  "text/xml",
                  GenerateDescriptor(request.LocalEndPoint.Address)
                  );
            }
            if (path == "contentDirectory.xml")
            {
                return new ResourceResponse(
                  HttpCode.Ok,
                  "text/xml",
                  "contentDirectory.xml"
                  );
            }
            if (path == "connectionManager.xml")
            {
                return new ResourceResponse(
                  HttpCode.Ok,
                  "text/xml",
                  "connectionManager.xml"
                  );
            }
            if (path == "MSMediaReceiverRegistrar.xml")
            {
                return new ResourceResponse(
                  HttpCode.Ok,
                  "text/xml",
                  "MSMediaReceiverRegistrar.xml"
                  );
            }
            if (path == "control")
            {
                return ProcessSoapRequest(request);
            }
            if (path.StartsWith("file/", StringComparison.Ordinal))
            {
                var id = path.Split('/')[1];
                Logger.LogTrace($"Serving file {id}");
                var item = GetItem(id, true) as IMediaResource;
                return new ItemResponse(Prefix, request, item);
            }
            if (path.StartsWith("cover/", StringComparison.Ordinal))
            {
                var id = path.Split('/')[1];
                Logger.LogTrace($"Serving cover {id}");
                var item = GetItem(id, false) as IMediaCover;
                if (item == null)
                {
                    throw new HttpStatusException(HttpCode.NotFound);
                }
                return new ItemResponse(Prefix, request, item.Cover, "Interactive");
            }
            if (path.StartsWith("subtitle/", StringComparison.Ordinal))
            {
                var id = path.Split('/')[1];
                Logger.LogTrace($"Serving subtitle {id}");
                var item = GetItem(id, false) as IMetaVideoItem;
                if (item == null)
                {
                    throw new HttpStatusException(HttpCode.NotFound);
                }
                return new ItemResponse(Prefix, request, item.Subtitle, "Background");
            }

            if (string.IsNullOrEmpty(path) || path == "index.html")
            {
                return new Redirect(request, Prefix + "index/0");
            }
            if (path.StartsWith("index/", StringComparison.Ordinal))
            {
                var id = path.Substring("index/".Length);
                var item = GetItem(id, false);
                return ProcessHtmlRequest(item);
            }
            if (request.Method == "SUBSCRIBE")
            {
                var res = new StringResponse(HttpCode.Ok, string.Empty);
                res.Headers.Add("SID", $"uuid:{Guid.NewGuid()}");
                res.Headers.Add("TIMEOUT", request.Headers["timeout"]);
                return res;
            }
            if (request.Method == "UNSUBSCRIBE")
            {
                return new StringResponse(HttpCode.Ok, string.Empty);
            }
            Logger.LogTrace($"Did not understand {request.Method} {path}");
            throw new HttpStatusException(HttpCode.NotFound);
        }

        private void ChangedServer(object sender, EventArgs e)
        {
            soapCache.Clear();
            Logger.LogTrace($"Rescanned mount {UUID}");
            systemID++;
        }

        private string GenerateDescriptor(IPAddress source)
        {
            var doc = new XmlDocument();
            doc.LoadXml(Encoding.UTF8.GetString(ResourceHelper.GetResourceData("description.xml") ?? new byte[0]));
            Guid guid;
            guidsForAddresses.TryGetValue(source, out guid);
            doc.SelectSingleNode("//*[local-name() = 'UDN']").InnerText = $"uuid:{guid}";
            doc.SelectSingleNode("//*[local-name() = 'modelNumber']").InnerText = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            doc.SelectSingleNode("//*[local-name() = 'friendlyName']").InnerText = FriendlyName;

            doc.SelectSingleNode(
              "//*[text() = 'urn:schemas-upnp-org:service:ContentDirectory:1']/../*[local-name() = 'SCPDURL']").InnerText =
              $"{Prefix}contentDirectory.xml";
            doc.SelectSingleNode(
              "//*[text() = 'urn:schemas-upnp-org:service:ContentDirectory:1']/../*[local-name() = 'controlURL']").InnerText =
              $"{Prefix}control";
            doc.SelectSingleNode("//*[local-name() = 'eventSubURL']").InnerText =
              $"{Prefix}events";

            doc.SelectSingleNode(
              "//*[text() = 'urn:schemas-upnp-org:service:ConnectionManager:1']/../*[local-name() = 'SCPDURL']").InnerText =
              $"{Prefix}connectionManager.xml";
            doc.SelectSingleNode(
              "//*[text() = 'urn:schemas-upnp-org:service:ConnectionManager:1']/../*[local-name() = 'controlURL']").InnerText
              =
              $"{Prefix}control";
            doc.SelectSingleNode(
              "//*[text() = 'urn:schemas-upnp-org:service:ConnectionManager:1']/../*[local-name() = 'eventSubURL']").InnerText
              =
              $"{Prefix}events";

            doc.SelectSingleNode(
              "//*[text() = 'urn:schemas-upnp-org:service:X_MS_MediaReceiverRegistrar:1']/../*[local-name() = 'SCPDURL']")
              .InnerText =
              $"{Prefix}MSMediaReceiverRegistrar.xml";
            doc.SelectSingleNode(
              "//*[text() = 'urn:schemas-upnp-org:service:X_MS_MediaReceiverRegistrar:1']/../*[local-name() = 'controlURL']")
              .InnerText =
              $"{Prefix}control";
            doc.SelectSingleNode(
              "//*[text() = 'urn:schemas-upnp-org:service:X_MS_MediaReceiverRegistrar:1']/../*[local-name() = 'eventSubURL']")
              .InnerText =
              $"{Prefix}events";

            return doc.OuterXml;
        }

        public void Preload()
        {
        }
    }
}