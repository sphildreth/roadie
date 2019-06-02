using System.Collections.Generic;

namespace Roadie.Library.Configuration
{
    public interface IRoadieSettings
    {
        Dictionary<string, List<string>> ArtistNameReplace { get; set; }
        string ConnectionString { get; set; }
        string ContentPath { get; set; }
        IConverting Converting { get; set; }
        string DefaultTimeZone { get; set; }
        string DiagnosticsPassword { get; set; }
        IEnumerable<string> DontDoMetaDataProvidersSearchArtists { get; set; }
        IEnumerable<string> FileExtensionsToDelete { get; set; }
        IFilePlugins FilePlugins { get; set; }
        string InboundFolder { get; set; }
        IIntegrations Integrations { get; set; }
        IImageSize LargeImageSize { get; set; }
        IImageSize MaximumImageSize { get; set; }
        string LibraryFolder { get; set; }
        string ListenAddress { get; set; }
        IImageSize MediumImageSize { get; set; }
        IProcessing Processing { get; set; }
        bool RecordNoResultSearches { get; set; }
        IRedisCache Redis { get; set; }
        string SecretKey { get; set; }
        string SingleArtistHoldingFolder { get; set; }
        string SiteName { get; set; }
        IImageSize SmallImageSize { get; set; }
        string SmtpFromAddress { get; set; }
        string SmtpHost { get; set; }
        string SmtpPassword { get; set; }
        int SmtpPort { get; set; }
        string SmtpUsername { get; set; }
        bool SmtpUseSSl { get; set; }
        IImageSize ThumbnailImageSize { get; set; }
        Dictionary<string, string> TrackPathReplace { get; set; }
        bool UseSSLBehindProxy { get; set; }
        string BehindProxyHost { get; set; }
        string WebsocketAddress { get; set; }
        IInspector Inspector { get; set; }
    }
}