using System;

namespace Roadie.Library.Models
{
    public sealed class TrackStreamInfo
    {
        public DataToken Track { get; set; }
        public string AcceptRanges
        {
            get
            {
                return "bytes";
            }
        }

        public string CacheControl { get; set; }
        public string ContentDisposition { get; set; }
        public string ContentDuration { get; set; }
        public string ContentLength { get; set; }
        public string ContentRange { get; set; }

        public string ContentType
        {
            get
            {
                return "audio/mpeg";
            }
        }

        public string Etag { get; set; }
        public string Expires { get; set; }
        public bool IsEndRangeRequest { get; set; }
        public bool IsFullRequest { get; set; }
        public string LastModified { get; set; }
        public byte[] Bytes { get; set; }
        public long BeginBytes { get; set; }
        public long EndBytes { get; set; }
        public string FileName { get; set; }

        public override string ToString()
        {
            return $"TrackId [{ this.Track }], Begin [{ this.BeginBytes }], End [{ this.EndBytes }]";
        }
    }
}