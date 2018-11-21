using Roadie.Library.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("release")]
    public partial class Release : EntityBase
    {
        [Column("alternateNames", TypeName = "text")]
        [MaxLength(65535)]
        public string AlternateNames { get; set; }

        [Column("amgId")]
        [MaxLength(50)]
        public string AmgId { get; set; }

        public Artist Artist { get; set; }

        [Column("artistId")]
        [Required]
        public int ArtistId { get; set; }

        public ICollection<CollectionRelease> Collections { get; set; }

        [Column("discogsId")]
        [MaxLength(50)]
        public string DiscogsId { get; set; }

        public ICollection<ReleaseGenre> Genres { get; set; }

        public ICollection<Image> Images { get; set; }

        [Column("isVirtual")]
        public bool? IsVirtual { get; set; }

        [Column("itunesId")]
        [MaxLength(100)]
        public string ITunesId { get; set; }

        public ICollection<ReleaseLabel> Labels { get; set; }

        [Column("lastFMId")]
        [MaxLength(50)]
        public string LastFMId { get; set; }

        [Column("lastFMSummary", TypeName = "text")]
        [MaxLength(65535)]
        public string LastFMSummary { get; set; }

        [Column("lastPlayed")]
        public DateTime? LastPlayed { get; set; }

        [Column("libraryStatus")]
        public LibraryStatus? LibraryStatus { get; set; }

        [Column("mediaCount")]
        public short? MediaCount { get; set; }

        public ICollection<ReleaseMedia> Medias { get; set; }

        [Column("musicBrainzId")]
        [MaxLength(100)]
        public string MusicBrainzId { get; set; }

        [Column("playedCount")]
        public int? PlayedCount { get; set; }

        [Column("profile", TypeName = "text")]
        [MaxLength(65535)]
        public string Profile { get; set; }

        [Column("rating")]
        public short? Rating { get; set; }

        [Column("releaseDate")]
        public DateTime? ReleaseDate { get; set; }

        [Column("releaseType")]
        public ReleaseType? ReleaseType { get; set; }

        [Column("spotifyId")]
        [MaxLength(100)]
        public string SpotifyId { get; set; }

        [Column("submissionId")]
        public int? SubmissionId { get; set; }

        [Column("tags", TypeName = "text")]
        [MaxLength(65535)]
        public string Tags { get; set; }

        [Column("thumbnail", TypeName = "blob")]
        public byte[] Thumbnail { get; set; }

        [MaxLength(250)]
        [Column("title")]
        [Required]
        public string Title { get; set; }

        [Column("trackCount")]
        public short TrackCount { get; set; }

        [Column("urls", TypeName = "text")]
        [MaxLength(65535)]
        public string URLs { get; set; }
    }
}