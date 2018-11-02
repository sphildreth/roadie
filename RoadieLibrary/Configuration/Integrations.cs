using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace roadie.Library.Setttings
{
    public class Integrations
    {
        private bool _lastFmEnabled = true;
        private bool _discogsEnabled = true;

        private string _lastFMApiKey = null;
        private string _lastFMSecret = null;

        private string _discogsConsumerKey = null;
        private string _discogsConsumerSecret = null;

        public string LastFMApiKey
        {
            get
            {
                if (string.IsNullOrEmpty(this._lastFMApiKey))
                {
                    // TODO
                    //var keySetting = SettingsHelper.Instance.ApiKeys.FirstOrDefault(x => x.ApiName == "LastFMApiKey");
                    //if (keySetting != null)
                    //{
                    //    this._lastFMApiKey = keySetting.Key;
                    //    this._lastFMSecret = keySetting.Secret;
                    //}
                    //else
                    //{
                    //    Trace.WriteLine("Unable To Find Api Key with Key Name of 'LastFMApiKey', Last FM Integration Disabled");
                    //}
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
                    // TODO
                    //var keySetting = SettingsHelper.Instance.ApiKeys.FirstOrDefault(x => x.ApiName == "LastFMApiKey");
                    //if (keySetting != null)
                    //{
                    //    this._lastFMApiKey = keySetting.Key;
                    //    this._lastFMSecret = keySetting.Secret;
                    //}
                    //else
                    //{
                    //    Trace.WriteLine("Unable To Find Api Key with Key Name of 'LastFMApiKey', Last FM Integration Disabled");
                    //}
                }
                return this._lastFMSecret;
            }

        }

        public string DiscogsConsumerKey
        {
            get
            {
                if (string.IsNullOrEmpty(this._discogsConsumerKey))
                {
                    // TODO
                    //var keySetting = SettingsHelper.Instance.ApiKeys.FirstOrDefault(x => x.ApiName == "DiscogsConsumerKey");
                    //if (keySetting != null)
                    //{
                    //    this._discogsConsumerKey = keySetting.Key;
                    //    this._discogsConsumerSecret = keySetting.Secret;
                    //}
                    //else
                    //{
                    //    Trace.WriteLine("Unable To Find Api Key with Key Name of 'DiscogsConsumerKey', Discogs Integration Disabled");
                    //}
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
                    // TODO
                    //var keySetting = SettingsHelper.Instance.ApiKeys.FirstOrDefault(x => x.ApiName == "DiscogsConsumerKey");
                    //if (keySetting != null)
                    //{
                    //    this._discogsConsumerKey = keySetting.Key;
                    //    this._discogsConsumerSecret = keySetting.Secret;
                    //}
                    //else
                    //{
                    //    Trace.WriteLine("Unable To Find Api Key with Key Name of 'DiscogsConsumerKey', Discogs Integration Disabled");
                    //}
                }
                return this._discogsConsumerSecret;
            }

        }

        public short? DiscogsReadWriteTimeout { get; set; }
        public short? DiscogsTimeout { get; set; }

        public bool ITunesProviderEnabled { get; set; }
        public bool MusicBrainzProviderEnabled { get; set; }
        public bool SpotifyProviderEnabled { get; set; }
        public bool DiscogsProviderEnabled
        {
            get
            {
                if(string.IsNullOrEmpty(this.DiscogsConsumerKey) || string.IsNullOrEmpty(this.DiscogsConsumerSecret))
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

        public Integrations()
        {
            this.ITunesProviderEnabled = true;
            this.MusicBrainzProviderEnabled = true;
            this.SpotifyProviderEnabled = true;

        }
    }
}
