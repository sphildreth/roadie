using Roadie.Dlna.Server;
using System.IO;

namespace Roadie.Dlna.Thumbnails
{
    internal interface IThumbnailLoader
    {
        DlnaMediaTypes Handling { get; }

        MemoryStream GetThumbnail(object item, ref int width, ref int height);
    }
}