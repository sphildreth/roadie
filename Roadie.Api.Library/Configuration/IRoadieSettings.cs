using System.Collections.Generic;

namespace Roadie.Library.Configuration
{
    public interface IRoadieSettings
    {
        Dictionary<string, IEnumerable<string>> ArtistNameReplace { get; set; }
        string BehindProxyHost { get; set; }
        string CollectionImageFolder { get; }
        string ConnectionString { get; set; }
        string ContentPath { get; set; }
        Converting Converting { get; set; }
        short DefaultRowsPerPage { get; set; }
        string DefaultTimeZone { get; set; }
        Dlna Dlna { get; set; }
        IEnumerable<string> DontDoMetaDataProvidersSearchArtists { get; set; }
        IEnumerable<string> FileExtensionsToDelete { get; set; }
        FilePlugins FilePlugins { get; set; }
        string GenreImageFolder { get; }
        string ImageFolder { get; set; }
        string InboundFolder { get; set; }
        Inspector Inspector { get; set; }
        Integrations Integrations { get; set; }
        bool IsRegistrationClosed { get; set; }
        string LabelImageFolder { get; }
        ImageSize LargeImageSize { get; set; }
        string LibraryFolder { get; set; }
        string ListenAddress { get; set; }
        ImageSize MaximumImageSize { get; set; }
        ImageSize MediumImageSize { get; set; }
        string PlaylistImageFolder { get; }
        Processing Processing { get; set; }
        bool RecordNoResultSearches { get; set; }
        RedisCache Redis { get; set; }
        string SearchEngineReposFolder { get; set; }
        string SecretKey { get; set; }
        string SiteName { get; set; }
        ImageSize SmallImageSize { get; set; }
        string SmtpFromAddress { get; set; }
        string SmtpHost { get; set; }
        string SmtpPassword { get; set; }
        int SmtpPort { get; set; }
        string SmtpUsername { get; set; }
        bool SmtpUseSSl { get; set; }
        short? SubsonicRatingBoost { get; set; }
        ImageSize ThumbnailImageSize { get; set; }
        Dictionary<string, string> TrackPathReplace { get; set; }
        bool UseRegistrationTokens { get; set; }
        string UserImageFolder { get; }
        bool UseSSLBehindProxy { get; set; }
        string WebsocketAddress { get; set; }
    }
}