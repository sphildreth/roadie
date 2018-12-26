using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Roadie.Library.Configuration
{
    public class Integrations
    {
        private string _discogsConsumerKey = null;
        private string _discogsConsumerSecret = null;
        private bool _discogsEnabled = true;
        private string _lastFMApiKey = null;
        private bool _lastFmEnabled = true;
        private string _lastFMSecret = null;
        public List<ApiKey> ApiKeys { get; set; }

        public string DiscogsConsumerKey
        {
            get
            {
                if (string.IsNullOrEmpty(this._discogsConsumerKey))
                {
                    var keySetting = this.ApiKeys.FirstOrDefault(x => x.ApiName == "DiscogsConsumerKey");
                    if (keySetting != null)
                    {
                        this._discogsConsumerKey = keySetting.Key;
                        this._discogsConsumerSecret = keySetting.KeySecret;
                    }
                    else
                    {
                        Trace.WriteLine("Unable To Find Api Key with Key Name of 'DiscogsConsumerKey', Discogs Integration Disabled");
                    }
                }
                return this._discogsConsumerKey;
            }
        }

        public string DiscogsConsumerSecret
        {
            get
            {
                if (string.IsNullOrEmpty(this._discogsConsumerSecret))
                {
                    var keySetting = this.ApiKeys.FirstOrDefault(x => x.ApiName == "DiscogsConsumerKey");
                    if (keySetting != null)
                    {
                        this._discogsConsumerKey = keySetting.Key;
                        this._discogsConsumerSecret = keySetting.KeySecret;
                    }
                    else
                    {
                        Trace.WriteLine("Unable To Find Api Key with Key Name of 'DiscogsConsumerKey', Discogs Integration Disabled");
                    }
                }
                return this._discogsConsumerSecret;
            }
        }

        public bool DiscogsProviderEnabled
        {
            get
            {
                if (string.IsNullOrEmpty(this.DiscogsConsumerKey) || string.IsNullOrEmpty(this.DiscogsConsumerSecret))
                {
                    return false;
                }
                return this._discogsEnabled;
            }
            set
            {
                this._discogsEnabled = value;
            }
        }

        public short? DiscogsReadWriteTimeout { get; set; }

        public short? DiscogsTimeout { get; set; }

        public bool ITunesProviderEnabled { get; set; }

        public string LastFMApiKey
        {
            get
            {
                if (string.IsNullOrEmpty(this._lastFMApiKey))
                {
                    var keySetting = this.ApiKeys.FirstOrDefault(x => x.ApiName == "LastFMApiKey");
                    if (keySetting != null)
                    {
                        this._lastFMApiKey = keySetting.Key;
                        this._lastFMSecret = keySetting.KeySecret;
                    }
                    else
                    {
                        Trace.WriteLine("Unable To Find Api Key with Key Name of 'LastFMApiKey', Last FM Integration Disabled");
                    }
                }
                return this._lastFMApiKey;
            }
        }

        public string LastFmApiSecret
        {
            get
            {
                if (string.IsNullOrEmpty(this._lastFMSecret))
                {
                    var keySetting = this.ApiKeys.FirstOrDefault(x => x.ApiName == "LastFMApiKey");
                    if (keySetting != null)
                    {
                        this._lastFMApiKey = keySetting.Key;
                        this._lastFMSecret = keySetting.KeySecret;
                    }
                    else
                    {
                        Trace.WriteLine("Unable To Find Api Key with Key Name of 'LastFMApiKey', Last FM Integration Disabled");
                    }
                }
                return this._lastFMSecret;
            }
        }

        public bool LastFmProviderEnabled
        {
            get
            {
                if (string.IsNullOrEmpty(this.LastFMApiKey) || string.IsNullOrEmpty(this.LastFmApiSecret))
                {
                    return false;
                }
                return this._lastFmEnabled;
            }
            set
            {
                this._lastFmEnabled = value;
            }
        }

        public bool MusicBrainzProviderEnabled { get; set; }
        public bool SpotifyProviderEnabled { get; set; }
    }
}