using System;

namespace Roadie.Library.Scrobble
{
    [Serializable]
    public class ScrobbleInfo
    {
        public string ArtistName { get; set; }
        public TimeSpan ElapsedTimeOfTrackPlayed => DateTime.UtcNow.Subtract(TimePlayed);
        public bool IsRandomizedScrobble { get; set; }

        public string ReleaseTitle { get; set; }
        public DateTime TimePlayed { get; set; }
        public TimeSpan TrackDuration { get; set; }
        public Guid TrackId { get; set; }
        public string TrackNumber { get; set; }
        public string TrackTitle { get; set; }

        public override string ToString()
        {
            return $"Artist: [{ArtistName}], Release: [{ReleaseTitle}], Track# [{TrackNumber}], Track [{TrackTitle}]";
        }
    }
}