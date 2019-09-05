using System.Net;

namespace Roadie.Dlna.Server
{
    public interface IRequest
    {
        string Body { get; }

        IHeaders Headers { get; }

        IPEndPoint LocalEndPoint { get; }

        string Method { get; }

        string Path { get; }

        IPEndPoint RemoteEndpoint { get; }
    }
}