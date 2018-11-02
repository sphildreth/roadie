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

        [Column("bandStatus", TypeName = "enum")]
        public BandStatus? BandStatus { get; set; }

        [Column("bioContext", TypeName = "text")]
        [MaxLength(65535)]
        public string BioContext { get; set; }

        [Column("birthDate", TypeName = "date")]
        public DateTime? BirthDate { get; set; }

        [Column("discogsId")]
        [MaxLength(50)]
        public string DiscogsId { get; set; }

        [Column("isniList", TypeName = "text")]
        [MaxLength(65535)]
        public string ISNIList { get; set; }

        [Column("iTunesId")]
        [MaxLength(100)]
        public string ITunesId { get; set; }

        [Column("musicBrainzId")]
        [MaxLength(100)]
        public string MusicBrainzId { get; set; }

        [Column("profile", TypeName = "text")]
        [MaxLength(65535)]
        public string Profile { get; set; }

        [Column("rating")]
        public short? Rating { get; set; }

        [Column("realName")]
        [MaxLength(500)]
        public string RealName { get; set; }

        public List<Release> Releases { get; set; }

        [Column("spotifyId")]
        [MaxLength(100)]
        public string SpotifyId { get; set; }

        [Column("thumbnail", TypeName = "blob")]
        public byte[] Thumbnail { get; set; }
    }
}