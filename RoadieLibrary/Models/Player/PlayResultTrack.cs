using Roadie.Library.Data;
using Roadie.Library.Models.Releases;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Player
{
    [Serializable]
    public class PlayResultTrack
    {
        private readonly string _baseUrl = null;

        public ArtistList Artist { get; set; }

        public ReleaseList Release { get; set; }

        public TrackList Track { get; set; }

        public UserTrack UserTrack { get; set; }

        public int? Duration
        {
            get
            {
                return this.Track.Duration;
            }
        }

        public string StreamUrl
        {
            get
            {
                // The cb paramneter is because Firefox caches this and prevents playcount from being updated
                return $"{ this._baseUrl}/play/stream/{ this.Track.Id }/?cb=" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            }
        }

        public string ShowArtistId
        {
            get
            {
                return this.Track.TrackArtist?.Text ?? this.Artist.Artist.Text;
            }
        }

        public string ShowArtistName
        {
            get
            {
                if (this.Track.TrackArtist != null)
                {
                    return this.Track.TrackArtist.Text;
                }
                return this.Artist.Artist.Text;
            }
        }

        public PlayResultTrack(string baseUrl)
        {
            this._baseUrl = baseUrl;
        }
    }
}
