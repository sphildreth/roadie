using Roadie.Library.Data;
using Roadie.Library.Models.Releases;
using System;

namespace Roadie.Library.Models.Player
{
    [Serializable]
    public class PlayResultTrack
    {
        private readonly string _baseUrl;

        public ArtistList Artist { get; set; }

        public int? Duration => Track.Duration;

        public ReleaseList<TrackList> Release { get; set; }

        public string ShowArtistId => Track.TrackArtist?.Artist.Text ?? Artist.Artist.Text;

        public string ShowArtistName
        {
            get
            {
                if (Track.TrackArtist != null) return Track.TrackArtist.Artist.Text;
                return Artist.Artist.Text;
            }
        }

        public string StreamUrl =>
            $"{_baseUrl}/play/stream/{Track.Id}/?cb=" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

        public TrackList Track { get; set; }

        public UserTrack UserTrack { get; set; }

        public PlayResultTrack(string baseUrl)
        {
            _baseUrl = baseUrl;
        }
    }
}