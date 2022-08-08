namespace Roadie.Library.MetaData.Audio
{
    public sealed class AudioMetaDataImage
    {
        public byte[] Data { get; set; }

        public string Description { get; set; }

        public string MimeType { get; set; }

        public AudioMetaDataImageType Type { get; set; }

        public string Url { get; set; }
    }
}
