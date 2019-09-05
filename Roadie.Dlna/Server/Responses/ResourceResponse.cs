using Roadie.Dlna.Utility;
using System;
using System.Diagnostics;
using System.IO;

namespace Roadie.Dlna.Server
{
    internal sealed class ResourceResponse : IResponse
    {
        private readonly byte[] resource;

        public Stream Body => new MemoryStream(resource);

        public IHeaders Headers { get; } = new ResponseHeaders();

        public HttpCode Status { get; }

        public ResourceResponse(HttpCode aStatus, string type, string aResource)
        {
            Status = aStatus;
            try
            {
                resource = ResourceHelper.GetResourceData(aResource);

                Headers["Content-Type"] = type;
                var len = resource?.Length.ToString() ?? "0";
                Headers["Content-Length"] = len;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to prepare resource { aResource }, Ex [{ ex }]");
                throw;
            }
        }
    }
}