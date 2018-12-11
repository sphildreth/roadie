using Newtonsoft.Json;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace Roadie.Library.Models
{
    [Serializable]
    public class TrackList : EntityInfoModelBase
    {
        public int? MediaNumber { get; set; }
        public int? TrackNumber { get; set; }
        public DataToken Track { get; set; }
        public ReleaseList Release { get; set; }
        public ArtistList Artist { get; set; }
        public ArtistList TrackArtist { get; set; }
        public string Title { get; set; }
        public int? Duration { get; set; }
        public string DurationTime
        {
            get
            {
                return this.Duration.HasValue ? new TimeInfo((decimal)this.Duration.Value).ToFullFormattedString() : "--:--";
            }
        }
        public string DurationTimeShort
        {
            get
            {
                return this.Duration.HasValue ? new TimeInfo((decimal)this.Duration.Value).ToShortFormattedString() : "--:--";
            }
        }
        public DateTime? LastPlayed { get; set; }
        public short? ReleaseRating { get; set; }
        public short? Rating { get; set; }
        public UserTrack UserRating { get; set; }
        public int? PlayedCount { get; set; }
        public int? FavoriteCount { get; set; }
        public Image Thumbnail { get; set; }
        public string TrackPlayUrl { get; set; }

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public string PartTitles { get; set; }

        public IEnumerable<string> PartTitlesList
        {
            get
            {
                if (string.IsNullOrEmpty(this.PartTitles))
                {
                    return null;
                }
                return this.PartTitles.Split('\n');
            }
        }

        [JsonIgnore]
        public DateTime? ReleaseDate { get; set; }
        public int? Year
        {
            get
            {
                if(this.ReleaseDate.HasValue)
                {
                    return this.ReleaseDate.Value.Year;
                }
                return null; 
            }
        }
        public int? FileSize { get; set; }


        public static TrackList FromDataTrack(Data.Track track, 
                                              int releaseMediaNumber,
                                              Data.Release release, 
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
                Title = track.Title,
                TrackPlayUrl = $"{ baseUrl }/play/track/{ track.RoadieId }.mp3",
                Thumbnail = trackThumbnail
            };

        }
    
    }
}
