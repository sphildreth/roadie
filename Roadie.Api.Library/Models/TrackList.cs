using Newtonsoft.Json;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using Release = Roadie.Library.Data.Release;
using Roadie.Library.Enums;

namespace Roadie.Library.Models
{
    [Serializable]
    [DebuggerDisplay("Trackid [{ TrackId }], Track Name [{ TrackName }}, Release Name [{ ReleaseName }]")]
    public class TrackList : EntityInfoModelBase
    {
        public ArtistList Artist { get; set; }
        public int? Duration { get; set; }

        public string DurationTime => Duration.HasValue ? new TimeInfo(Duration.Value).ToFullFormattedString() : "--:--";

        public string DurationTimeShort => Duration.HasValue ? new TimeInfo(Duration.Value).ToShortFormattedString() : "--:--";

        public int? FavoriteCount { get; set; }
        public int? FileSize { get; set; }
        public DateTime? LastPlayed { get; set; }
        public int? MediaNumber { get; set; }

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public string PartTitles { get; set; }

        public IEnumerable<string> PartTitlesList
        {
            get
            {
                if (string.IsNullOrEmpty(PartTitles)) return null;
                return PartTitles.Split('\n');
            }
        }

        public int? PlayedCount { get; set; }
        public short? Rating { get; set; }
        public ReleaseList Release { get; set; }
        [JsonIgnore] public DateTime? ReleaseDate { get; set; }
        public short? ReleaseRating { get; set; }
        public Image Thumbnail { get; set; }
        public string Title { get; set; }
        public DataToken Track { get; set; }
        public ArtistList TrackArtist { get; set; }
        [JsonIgnore] public string TrackId => Track?.Value;

        [JsonIgnore] public string TrackName => Track?.Text;
        [JsonIgnore] public string ReleaseName => Release?.Release?.Text;
        public Statuses? Status { get; set; }
        public string StatusVerbose => (Status ?? Statuses.Missing).ToString();
        public int? TrackNumber { get; set; }
        public string TrackPlayUrl { get; set; }
        public UserTrack UserRating { get; set; }

        public int? Year
        {
            get
            {
                if (ReleaseDate.HasValue) return ReleaseDate.Value.Year;
                return null;
            }
        }

        public int GetArtistReleaseHashCode() => this.Artist?.Artist?.Value.GetHashCode() + this.Release?.Release?.Value.GetHashCode() ?? 0;

        public override int GetHashCode() => this.Artist?.Artist?.Value.GetHashCode() + this.Release?.Release?.Value.GetHashCode() + this.Track?.Value.GetHashCode() ?? 0;

        public static IEnumerable<int> AllIndexesOfArtist(IEnumerable<TrackList> tracks, string artistId)
        {
            if(tracks == null || !tracks.Any())
            {
                return new int[0];
            }
            return tracks.Select((b, i) => b.Artist?.Artist?.Value == artistId ? i : -1).Where(i => i != -1).ToArray();            
        }

        public static IEnumerable<int> AllIndexesOfRelease(IEnumerable<TrackList> tracks, string releaseId)
        {
            if (tracks == null || !tracks.Any())
            {
                return new int[0];
            }
            return tracks.Select((b, i) => b.Release?.Release?.Value == releaseId ? i : -1).Where(i => i != -1).ToArray();
        }


        /// <summary>
        /// Ensure that the given list is sorted so that Artist and Release don't repeat in sequence.
        /// </summary>
        public static IEnumerable<TrackList> Shuffle(IEnumerable<TrackList> tracks)
        {
            
            var shuffledTracks = new List<TrackList>();
            var skippedTracks = new List<TrackList>();
            foreach(var track in tracks)
            {
                var trackArtist = track.Artist?.Artist?.Value;
                var trackRelease = track.Release?.Release?.Value;
                if (!shuffledTracks.Any(x => x.Artist?.Artist?.Value == trackArtist &&
                                             x.Release?.Release?.Value != trackRelease))
                {
                    shuffledTracks.Add(track);
                }
                else
                {
                    skippedTracks.Add(track);
                }
            }
            var result = new List<TrackList>(shuffledTracks);
            while (skippedTracks.ToList().Any())
            {
                var st = skippedTracks.First();
                var trackArtist = st.Artist?.Artist?.Value;
                var insertAt = AllIndexesOfArtist(result, trackArtist).Last() + 2;
                if(insertAt < result.Count() - 1)
                {
                    result.Insert(insertAt, st);
                    skippedTracks.Remove(st);
                }
            }
            return result;
        }


        public static TrackList FromDataTrack(string trackPlayUrl,
            Data.Track track,
            int releaseMediaNumber,
            Release release,
            Data.Artist artist,
            Data.Artist trackArtist,
            string baseUrl,
            Image trackThumbnail,
            Image releaseThumbnail,
            Image artistThumbnail,
            Image trackArtistThumbnail)
        {
            return new TrackList
            {
                DatabaseId = track.Id,
                Id = track.RoadieId,
                Track = new DataToken
                {
                    Text = track.Title,
                    Value = track.RoadieId.ToString()
                },
                Release = ReleaseList.FromDataRelease(release, artist, baseUrl, artistThumbnail, releaseThumbnail),
                LastPlayed = track.LastPlayed,
                Artist = ArtistList.FromDataArtist(artist, artistThumbnail),
                TrackArtist = trackArtist == null ? null : ArtistList.FromDataArtist(trackArtist, trackArtistThumbnail),
                TrackNumber = track.TrackNumber,
                MediaNumber = releaseMediaNumber,
                CreatedDate = track.CreatedDate,
                LastUpdated = track.LastUpdated,
                Duration = track.Duration,
                FileSize = track.FileSize,
                ReleaseDate = release.ReleaseDate,
                PlayedCount = track.PlayedCount,
                Rating = track.Rating,
                Status = track.Status,
                Title = track.Title,
                TrackPlayUrl = trackPlayUrl,
                Thumbnail = trackThumbnail
            };
        }


    }
}