using Roadie.Dlna.Server;
using Roadie.Dlna.Server.Metadata;
using System;
using System.IO;

namespace Roadie.Dlna.Services
{
    public sealed class CoverArt : IMediaCoverResource, IMetaInfo
    {
        private byte[] bytes;
        public IMediaCoverResource Cover => this;

        public string Id
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public DateTime InfoDate { get; }
        public long? InfoSize { get; }
        public DlnaMediaTypes MediaType => DlnaMediaTypes.Image;

        public int? MetaHeight { get; }
        public int? MetaWidth { get; }
        public string Path => throw new NotImplementedException();
        public string PN => "JPEG_TN";

        public IHeaders Properties => throw new NotImplementedException();
        public string Title => throw new NotImplementedException();
        public DlnaMime Type => DlnaMime.ImageJPEG;

        public CoverArt(byte[] data, int width, int height)
        {
            bytes = data;
            MetaWidth = width;
            MetaHeight = height;
        }

        public int CompareTo(IMediaItem other) => throw new NotImplementedException();

        public Stream CreateContentStream() => new MemoryStream(bytes);

        public bool Equals(IMediaItem other) => throw new NotImplementedException();

        public string ToComparableTitle() => throw new NotImplementedException();
    }
}