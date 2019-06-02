using System.Collections.Generic;

namespace Roadie.Library.Configuration
{
    public interface IIntegrations
    {
        List<ApiKey> ApiKeys { get; set; }
        string DiscogsConsumerKey { get; }
        string DiscogsConsumerSecret { get; }
        bool DiscogsProviderEnabled { get; set; }
        short? DiscogsReadWriteTimeout { get; set; }
        short? DiscogsTimeout { get; set; }
        bool ITunesProviderEnabled { get; set; }
        string LastFMApiKey { get; }
        string LastFmApiSecret { get; }
        bool LastFmProviderEnabled { get; set; }
        bool MusicBrainzProviderEnabled { get; set; }
        bool SpotifyProviderEnabled { get; set; }
    }
}