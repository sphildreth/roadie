using Newtonsoft.Json;
using Roadie.Library.Models.Collections;
using Roadie.Library.Models.Playlists;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Statistics;
using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Roadie.Library.Models
{
    [Serializable]
    public class Artist : EntityModelBase
    {
        public const string DefaultIncludes =
            "stats,images,associatedartists,similarartists,comments,collections,playlists,contributions,labels";

        public IEnumerable<ReleaseList> ArtistContributionReleases;
        public IEnumerable<LabelList> ArtistLabels;
        private IEnumerable<string> _isniList;
        [MaxLength(100)] public string AmgId { get; set; }

        public string ArtistType { get; set; }

        public IEnumerable<ArtistList> AssociatedArtists { get; set; }

        public IEnumerable<DataToken> AssociatedArtistsTokens { get; set; }

        public string BandStatus { get; set; }

        [MaxLength(65535)] public string BioContext { get; set; }

        public DateTime? BirthDate { get; set; }

        public IEnumerable<CollectionList> CollectionsWithArtistReleases { get; set; }

        public IEnumerable<Comment> Comments { get; set; }

        [MaxLength(50)] public string DiscogsId { get; set; }

        public IEnumerable<DataToken> Genres { get; set; }

        public IEnumerable<Image> Images { get; set; }

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public string ISNI { get; set; }

        [JsonProperty("isniList")]
        public IEnumerable<string> ISNIList
        {
            get
            {
                if (_isniList == null)
                {
                    if (string.IsNullOrEmpty(ISNI)) return null;
                    return ISNI.Split('|');
                }

                return _isniList;
            }
            set => _isniList = value;
        }

        [MaxLength(100)] public string ITunesId { get; set; }

        public Image MediumThumbnail { get; set; }

        [MaxLength(100)] public string MusicBrainzId { get; set; }

        [MaxLength(250)] public string Name { get; set; }

        /// <summary>
        ///     When populated a "data:image" base64 byte array of an image to use as secondary Artist images.
        /// </summary>
        public List<string> NewSecondaryImagesData { get; set; }

        /// <summary>
        ///     When populated a "data:image" base64 byte array of an image to use as new Thumbnail.
        /// </summary>
        public string NewThumbnailData { get; set; }

        public IEnumerable<PlaylistList> PlaylistsWithArtistReleases { get; set; }

        [MaxLength(65535)] public string Profile { get; set; }

        public decimal? Rank { get; set; }

        /// <summary>
        ///     The Position of this Artist as ranked against other Artists (highest ranking Artist is #1)
        /// </summary>
        public int? RankPosition { get; set; }

        public short? Rating { get; set; }

        [MaxLength(500)] public string RealName { get; set; }

        public IEnumerable<ReleaseList> Releases { get; set; }

        public IEnumerable<ArtistList> SimilarArtists { get; set; }

        public IEnumerable<DataToken> SimilarArtistsTokens { get; set; }

        [MaxLength(100)] public string SpotifyId { get; set; }

        public CollectionStatistics Statistics { get; set; }

        public Image Thumbnail { get; set; }

        public string Tooltip => Name;

        public UserArtist UserRating { get; set; }

        public Artist()
        {
            BandStatus = "1";
        }
    }
}