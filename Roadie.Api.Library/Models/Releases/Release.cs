using Roadie.Library.Models.Playlists;
using Roadie.Library.Models.Statistics;
using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public class Release : EntityModelBase
    {
        public const string DefaultIncludes = "tracks,stats,images,comments,collections,labels,playlists,genres";
        public const string DefaultListIncludes = "";

        [MaxLength(50)] public string AmgId { get; set; }

        public ArtistList Artist { get; set; }

        public List<ReleaseInCollection> Collections { get; set; }

        public IEnumerable<Comment> Comments { get; set; }
        [MaxLength(50)] public string DiscogsId { get; set; }

        public IEnumerable<DataToken> Genres { get; set; }
        public IEnumerable<Image> Images { get; set; }
        public bool? IsVirtual { get; set; }

        [MaxLength(100)] public string ITunesId { get; set; }

        public List<ReleaseLabel> Labels { get; set; }

        [MaxLength(50)] public string LastFMId { get; set; }

        [MaxLength(65535)] public string LastFMSummary { get; set; }

        public string LibraryStatus { get; set; }

        public short MaxMediaNumber { get; set; }
        public short? MediaCount { get; set; }

        public List<ReleaseMediaList> Medias { get; set; }

        public Image MediumThumbnail { get; set; }
        [MaxLength(100)] public string MusicBrainzId { get; set; }

        /// <summary>
        ///     When populated a "data:image" base64 byte array of an image to use as secondary Release images.
        /// </summary>
        public List<string> NewSecondaryImagesData { get; set; }

        // When populated a "data:image" base64 byte array of an image to use as new Thumbnail
        public string NewThumbnailData { get; set; }

        public IEnumerable<PlaylistList> Playlists { get; set; }

        [MaxLength(65535)] public string Profile { get; set; }

        public decimal? Rank { get; set; }

        /// <summary>
        ///     The Position of this Release as ranked against other Releases (highest ranking Release is #1)
        /// </summary>
        public int? RankPosition { get; set; }

        public short? Rating { get; set; }
        [Required] public DateTime ReleaseDate { get; set; }

        public string ReleasePlayUrl { get; set; }
        public string ReleaseType { get; set; }

        [MaxLength(100)] public string SpotifyId { get; set; }

        public ReleaseStatistics Statistics { get; set; }
        public ReleaseSubmission Submission { get; set; }

        public Image Thumbnail { get; set; }
        [MaxLength(250)] [Required] public string Title { get; set; }

        public short TrackCount { get; set; }
        public UserRelease UserRating { get; set; }

        public override string ToString()
        {
            return $"Id [{ Id }], Title [{ Title }]";
        }
    }
}