using System.IO;

namespace Roadie.Dlna.Server
{
    internal sealed class FileResponse : IResponse
    {
        private readonly FileInfo body;

        public Stream Body => body.OpenRead();

        public IHeaders Headers { get; } = new ResponseHeaders();

        public HttpCode Status { get; }

        public FileResponse(HttpCode aStatus, FileInfo aBody)
                              : this(aStatus, "text/html; charset=utf-8", aBody)
        {
        }

        public FileResponse(HttpCode aStatus, string aMime, FileInfo aBody)
        {
            Status = aStatus;
            body = aBody;

            Headers["Content-Type"] = aMime;
            Headers["Content-Length"] = body.Length.ToString();
        }
    }
}