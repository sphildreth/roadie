using System;
using System.Net;

namespace Roadie.Dlna.Server
{
    public sealed class HttpAuthorizationEventArgs : EventArgs
    {
        public bool Cancel { get; set; }

        public IHeaders Headers { get; private set; }

        public IPEndPoint RemoteEndpoint { get; private set; }

        internal HttpAuthorizationEventArgs(IHeaders headers,
                              IPEndPoint remoteEndpoint)
        {
            Headers = headers;
            RemoteEndpoint = remoteEndpoint;
        }
    }
}