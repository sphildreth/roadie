using System;

namespace Roadie.Library.Scrobble
{
    [Serializable]
    public class ScrobbleInfo
    {
        public string ArtistName { get; set; }
        public bool IsRandomizedScrobble { get; set; }

        public string ReleaseTitle { get; set; }
        public DateTime TimePlayed { get; set; }
        public Guid TrackId { get; set; }
        public string TrackNumber { get; set; }
        public string TrackTitle { get; set; }
        public TimeSpan TrackDuration { get; set; }

        public TimeSpan ElapsedTimeOfTrackPlayed
        {
            get
            {
                return DateTime.UtcNow.Subtract(TimePlayed);
            }
        }

        public override string ToString() => $"Artist: [{ ArtistName }], Release: [{ ReleaseTitle}], Track# [{ TrackNumber }], Track [{ TrackTitle }]";
    }
}