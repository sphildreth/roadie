using System;

namespace Roadie.Library.SearchEngines.Imaging
{
    [Serializable]
    public class ImageSearchResult
    {
        public __Metadata __metadata { get; set; }
        public string ID { get; set; }
        public string ArtistId { get; set; }
        public string ArtistName { get; set; }
        public string Title { get; set; }
        public string MediaUrl { get; set; }
        public string SourceUrl { get; set; }
        public string DisplayUrl { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string FileSize { get; set; }
        public string ContentType { get; set; }
        public Thumbnail Thumbnail { get; set; }
    }

    [Serializable]
    public class __Metadata
    {
        public string uri { get; set; }
        public string type { get; set; }
    }

    [Serializable]
    public class Thumbnail
    {
        public __Metadata __metadata { get; set; }
        public string MediaUrl { get; set; }
        public string ContentType { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string FileSize { get; set; }
    }
}