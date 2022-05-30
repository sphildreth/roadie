using System;
using System.Collections.Generic;
using System.IO;

namespace Roadie.Library.MetaData.Audio
{
    public interface IAudioMetaData
    {
        string AmgId { get; set; }

        string Artist { get; set; }

        string ArtistRaw { get; set; }

        int? AudioBitrate { get; set; }

        int? AudioChannels { get; set; }

        AudioMetaDataWeights AudioMetaDataWeights { get; }

        int? AudioSampleRate { get; set; }

        string Comments { get; set; }

        string Directory { get; }

        int? Disc { get; set; }

        string DiscSubTitle { get; set; }

        FileInfo FileInfo { get; }

        string Filename { get; set; }

        ICollection<string> Genres { get; set; }

        IEnumerable<AudioMetaDataImage> Images { get; set; }

        string ISRC { get; }

        bool IsSoundTrack { get; }

        bool IsValid { get; }

        string LastFmId { get; set; }

        string MusicBrainzId { get; set; }

        string Release { get; set; }

        string ReleaseLastFmId { get; set; }

        string ReleaseMusicBrainzId { get; set; }

        ulong? SampleLength { get; set; }

        string SpecialTitle { get; set; }

        string SpotifyId { get; set; }

        TimeSpan? Time { get; set; }

        string Title { get; set; }

        int? TotalDiscCount { get; set; }

        double TotalSeconds { get; }

        int? TotalTrackNumbers { get; set; }

        string TrackArtist { get; set; }

        string TrackArtistRaw { get; set; }

        IEnumerable<string> TrackArtists { get; }

        short? TrackNumber { get; set; }

        int ValidWeight { get; }

        int? Year { get; set; }

        bool Equals(object obj);

        int GetHashCode();

        void SetArtistName(string name);

        string ToString();
    }
}
