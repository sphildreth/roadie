using Roadie.Library.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("artist")]
    public partial class Artist : BeginAndEndNamedEntityBase
    {
        [Column("amgId")]
        [MaxLength(100)]
        public string AmgId { get; set; }

        [Column("artistType", TypeName = "enum")]
        public string ArtistType { get; set; }

        /// <summary>
        /// Artists who this Artist is Associated To
        /// </summary>
        [InverseProperty("Artist")]
        public virtual ICollection<ArtistAssociation> AssociatedArtists { get; set; }

        [Column("bandStatus", TypeName = "enum")]
        public BandStatus? BandStatus { get; set; }

        [Column("bioContext", TypeName = "text")]
        [MaxLength(65535)]
        public string BioContext { get; set; }

        [Column("birthDate", TypeName = "date")]
        public DateTime? BirthDate { get; set; }

        [InverseProperty("Artist")]
        public virtual ICollection<Comment> Comments { get; set; }

        [InverseProperty("Artist")]
        public virtual ICollection<Credit> Credits { get; set; }

        [Column("discogsId")]
        [MaxLength(50)]
        public string DiscogsId { get; set; }

        [InverseProperty("Artist")]
        public virtual ICollection<ArtistGenre> Genres { get; set; }

        /// <summary>
        /// Where the Artist is the Artist on the Track not on an Artist Release
        /// </summary>
        [InverseProperty("TrackArtist")]
        public virtual ICollection<Track> Tracks { get; set; }

        [Column("isniList", TypeName = "text")]
        [MaxLength(65535)]
        public string ISNI { get; set; }

        [Column("iTunesId")]
        [MaxLength(100)]
        public string ITunesId { get; set; }

        [Column("lastPlayed")]
        public DateTime? LastPlayed { get; set; }

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

        [Column("realName")]
        [MaxLength(500)]
        public string RealName { get; set; }

        [Column("releaseCount")]
        public int? ReleaseCount { get; set; }

        public virtual ICollection<Release> Releases { get; set; }

        /// <summary>
        /// Artists who are similiar to this Artist
        /// </summary>
        [InverseProperty("Artist")]
        public virtual ICollection<ArtistSimilar> SimilarArtists { get; set; }

        [Column("spotifyId")]
        [MaxLength(100)]
        public string SpotifyId { get; set; }

        [Column("trackCount")]
        public int? TrackCount { get; set; }

        [InverseProperty("Artist")]
        public virtual ICollection<UserArtist> UserArtists { get; set; }

        public Artist()
        {
            Releases = new HashSet<Release>();
            Genres = new HashSet<ArtistGenre>();
            AssociatedArtists = new HashSet<ArtistAssociation>();
            SimilarArtists = new HashSet<ArtistSimilar>();
            Comments = new HashSet<Comment>();
            UserArtists = new HashSet<UserArtist>();

            Rating = 0;
            Status = Statuses.Ok;
        }
    }
}