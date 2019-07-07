using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Roadie.Library.Configuration
{
    public class Integrations : IIntegrations
    {
        private bool _discogsEnabled = true;
        private bool _bingSearchEnabled = true;
        private bool _lastFmEnabled = true;

        public List<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();

        public string DiscogsConsumerKey
        {
            get
            {
                var keySetting = ApiKeys.FirstOrDefault(x => x.ApiName == "DiscogsConsumerKey");
                if (keySetting != null)
                    return keySetting.Key;
                Trace.WriteLine(
                    "Unable To Find Api Key with Key Name of 'DiscogsConsumerKey', Discogs Integration Disabled");
                return null;
            }
        }

        public string DiscogsConsumerSecret
        {
            get
            {
                var keySetting = ApiKeys.FirstOrDefault(x => x.ApiName == "DiscogsConsumerKey");
                if (keySetting != null)
                    return keySetting.KeySecret;
                Trace.WriteLine(
                    "Unable To Find Api Key with Key Name of 'DiscogsConsumerKey', Discogs Integration Disabled");
                return null;
            }
        }

        public bool DiscogsProviderEnabled
        {
            get
            {
                if (string.IsNullOrEmpty(DiscogsConsumerKey) || string.IsNullOrEmpty(DiscogsConsumerSecret))
                    return false;
                return _discogsEnabled;
            }
            set => _discogsEnabled = value;
        }

        public short? DiscogsReadWriteTimeout { get; set; }

        public short? DiscogsTimeout { get; set; }

        public bool ITunesProviderEnabled { get; set; }

        public bool BingImageSearchEngineEnabled
        {
            get
            {
                var apiKey = ApiKeys.FirstOrDefault(x => x.ApiName == "BingImageSearch");
                if (string.IsNullOrEmpty(apiKey?.Key))
                {
                    return false;
                }
                return _bingSearchEnabled;
            }
            set => _bingSearchEnabled = value;
        }


        public string LastFMApiKey
        {
            get
            {
                var keySetting = ApiKeys.FirstOrDefault(x => x.ApiName == "LastFMApiKey");
                if (keySetting != null)
                    return keySetting.Key;
                Trace.WriteLine("Unable To Find Api Key with Key Name of 'LastFMApiKey', Last FM Integration Disabled");
                return null;
            }
        }

        public string LastFmApiSecret
        {
            get
            {
                var keySetting = ApiKeys.FirstOrDefault(x => x.ApiName == "LastFMApiKey");
                if (keySetting != null)
                    return keySetting.KeySecret;
                Trace.WriteLine("Unable To Find Api Key with Key Name of 'LastFMApiKey', Last FM Integration Disabled");
                return null;
            }
        }

        public bool LastFmProviderEnabled
        {
            get
            {
                if (string.IsNullOrEmpty(LastFMApiKey) || string.IsNullOrEmpty(LastFmApiSecret)) return false;
                return _lastFmEnabled;
            }
            set => _lastFmEnabled = value;
        }

        public bool MusicBrainzProviderEnabled { get; set; }
        public bool SpotifyProviderEnabled { get; set; }

        public Integrations()
        {
            DiscogsReadWriteTimeout = short.MaxValue;
            DiscogsTimeout = short.MaxValue;
        }
    }
}