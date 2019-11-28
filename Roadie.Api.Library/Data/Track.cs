using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("track")]
    public partial class Track : EntityBase
    {
        [Column("alternateNames", TypeName = "text")]
        [MaxLength(65535)]
        public string AlternateNames { get; set; }

        [Column("amgId")]
        [MaxLength(50)]
        public string AmgId { get; set; }

        [Column("artistId")]
        public int? ArtistId { get; set; }

        [ForeignKey(nameof(ArtistId))]
        [InverseProperty("Tracks")]
        public virtual Artist TrackArtist { get; set; }

        [InverseProperty("Track")]
        public virtual ICollection<Comment> Comments { get; set; }

        [Column("duration")]
        public int? Duration { get; set; }

        [Column("fileName")]
        [MaxLength(500)]
        public string FileName { get; set; }

        [Column("filePath")]
        [MaxLength(1000)]
        public string FilePath { get; set; }

        [Column("fileSize")]
        public int? FileSize { get; set; }

        [Column("hash")]
        [MaxLength(32)]
        public string Hash { get; set; }

        [Column("isrc")]
        [MaxLength(15)]
        public string ISRC { get; set; }

        [Column("lastFMId")]
        [MaxLength(50)]
        public string LastFMId { get; set; }

        [Column("lastPlayed")]
        public DateTime? LastPlayed { get; set; }

        [Column("musicBrainzId")]
        [MaxLength(100)]
        public string MusicBrainzId { get; set; }

        [Column("partTitles", TypeName = "text")]
        [MaxLength(65535)]
        public string PartTitles { get; set; }

        [Column("playedCount")]
        public int? PlayedCount { get; set; }

        [Column("rating")]
        public short Rating { get; set; }

        public virtual ReleaseMedia ReleaseMedia { get; set; }

        [Column("releaseMediaId")]
        public int ReleaseMediaId { get; set; }

        [Column("spotifyId")]
        [MaxLength(100)]
        public string SpotifyId { get; set; }

        [Column("tags", TypeName = "text")]
        [MaxLength(65535)]
        public string Tags { get; set; }

        [MaxLength(250)]
        [Column("title")]
        [Required]
        public string Title { get; set; }

        [Column("trackNumber")]
        [Required]
        public short TrackNumber { get; set; }

        [InverseProperty("Track")]
        public virtual ICollection<Credit> Credits { get; set; }

        [InverseProperty("Track")]
        public virtual ICollection<PlaylistTrack> Playlists { get; set; }

        [InverseProperty("Track")]
        public virtual ICollection<UserQue> UserQues { get; set; }

        [InverseProperty("Track")]
        public virtual ICollection<UserTrack> UserTracks { get; set; }

        public Track()
        {
            Comments = new HashSet<Comment>();
            Credits = new HashSet<Credit>();
            Playlists = new HashSet<PlaylistTrack>();
            UserQues = new HashSet<UserQue>();
            UserTracks = new HashSet<UserTrack>();
            Rating = 0;
        }
    }
}