namespace Roadie.Library.Models
{
    public sealed class TrackStreamInfo
    {
        public string AcceptRanges => "bytes";
        public long BeginBytes { get; set; }
        public byte[] Bytes { get; set; }
        public string CacheControl { get; set; }
        public string ContentDisposition { get; set; }
        public string ContentDuration { get; set; }
        public string ContentLength { get; set; }
        public string ContentRange { get; set; }
        public string ContentType => "audio/mpeg";
        public long EndBytes { get; set; }
        public string Etag { get; set; }
        public string Expires { get; set; }
        public string FileName { get; set; }
        public bool IsEndRangeRequest { get; set; }
        public bool IsFullRequest { get; set; }
        public string LastModified { get; set; }
        public DataToken Track { get; set; }

        public override string ToString()
        {
            return $"TrackId [{Track}], ContentRange [{ContentRange}], Begin [{BeginBytes}], End [{EndBytes}]";
        }
    }
}