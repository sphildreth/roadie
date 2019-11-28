using Roadie.Library.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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

        public virtual Artist Artist { get; set; }

        [Column("artistId")]
        [Required]
        public int ArtistId { get; set; }

        [InverseProperty("Release")]
        public virtual ICollection<CollectionRelease> Collections { get; set; }

        [InverseProperty("Release")]
        public virtual ICollection<Comment> Comments { get; set; }

        [Column("discogsId")]
        [MaxLength(50)]
        public string DiscogsId { get; set; }

        [Column("duration")]
        public int? Duration { get; set; }

        [InverseProperty("Release")]
        public virtual ICollection<ReleaseGenre> Genres { get; set; }

        [NotMapped]
        public IEnumerable<Imaging.IImage> Images { get; set; } = Enumerable.Empty<Imaging.IImage>();

        public Imaging.IImage ThumbnailImage => Images.OrderBy(x => x.SortOrder).FirstOrDefault();

        [Column("isVirtual")]
        public bool? IsVirtual { get; set; }

        [Column("itunesId")]
        [MaxLength(100)]
        public string ITunesId { get; set; }

        [InverseProperty("Release")]
        public virtual ICollection<ReleaseLabel> Labels { get; set; }

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

        [InverseProperty("Release")]
        public virtual ICollection<ReleaseMedia> Medias { get; set; }

        [Column("musicBrainzId")]
        [MaxLength(100)]
        public string MusicBrainzId { get; set; }

        [Column("playedCount")]
        public int? PlayedCount { get; set; }

        [Column("profile", TypeName = "text")]
        [MaxLength(65535)]
        public string Profile { get; set; }

        [Column("rank")]
        public decimal? Rank { get; set; }

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

        [MaxLength(250)]
        [Column("title")]
        [Required]
        public string Title { get; set; }

        [MaxLength(250)]
        [Column("sortTitle")]
        public string SortTitle { get; set; }

        public string SortTitleValue => string.IsNullOrEmpty(SortTitle) ? Title : SortTitle;

        [Column("trackCount")]
        public short TrackCount { get; set; }

        [Column("urls", TypeName = "text")]
        [MaxLength(65535)]
        public string URLs { get; set; }

        [InverseProperty("Release")]
        public virtual ICollection<Credit> Credits { get; set; }

        [InverseProperty("Release")]
        public virtual ICollection<UserRelease> UserRelases { get; set; }

        public Release()
        {
            Rating = 0;

            ReleaseType = Enums.ReleaseType.Release;
            Medias = new HashSet<ReleaseMedia>();
            Labels = new HashSet<ReleaseLabel>();
            Collections = new HashSet<CollectionRelease>();
            Genres = new HashSet<ReleaseGenre>();
            Comments = new HashSet<Comment>();
            Credits = new HashSet<Credit>();
            UserRelases = new HashSet<UserRelease>();
        }
    }
}