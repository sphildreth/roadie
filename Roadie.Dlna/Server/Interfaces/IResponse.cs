using System.IO;

namespace Roadie.Dlna.Server
{
    internal interface IResponse
    {
        Stream Body { get; }

        IHeaders Headers { get; }

        HttpCode Status { get; }
    }
}