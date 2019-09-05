using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Roadie.Dlna.Server
{
    public sealed class HttpAuthorizer : IHttpAuthorizationMethod, IDisposable
    {
        private readonly List<IHttpAuthorizationMethod> methods =
          new List<IHttpAuthorizationMethod>();

        private readonly HttpServer server;

        public HttpAuthorizer()
        {
        }

        public HttpAuthorizer(HttpServer server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }
            this.server = server;
            server.OnAuthorizeClient += OnAuthorize;
        }

        public void AddMethod(IHttpAuthorizationMethod method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            methods.Add(method);
        }

        public bool Authorize(IHeaders headers, IPEndPoint endPoint)
        {
            if (methods.Count == 0)
            {
                return true;
            }
            try
            {
                return methods.Any(m => m.Authorize(headers, endPoint));
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to authorize [{ ex }]");
                return false;
            }
        }

        public void Dispose()
        {
            if (server != null)
            {
                server.OnAuthorizeClient -= OnAuthorize;
            }
        }

        private void OnAuthorize(object sender, HttpAuthorizationEventArgs e)
        {
            e.Cancel = !Authorize(
              e.Headers,
              e.RemoteEndpoint
              );
        }
    }
}