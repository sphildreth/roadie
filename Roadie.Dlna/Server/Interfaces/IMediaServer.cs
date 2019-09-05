using Roadie.Library.Configuration;
using System;

namespace Roadie.Dlna.Server
{
    public interface IMediaServer
    {
        void Preload();

        IHttpAuthorizationMethod Authorizer { get; }
        string FriendlyName { get; }

        Guid UUID { get; }

        IMediaItem GetItem(string id, bool isFileRequest);
    }
}