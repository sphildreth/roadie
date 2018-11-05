using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Data.Models
{
    [Serializable]
    public class Track : EntityModelBase
    {
        [MaxLength(50)]
        public string AmgId { get; set; }

        //        public Artist Artist { get; set; }

        public int ArtistId { get; set; }

        public int Duration { get; set; }

        [MaxLength(32)]
        public string Hash { get; set; }

        [MaxLength(15)]
        public string ISRC { get; set; }

        [MaxLength(50)]
        public string LastFMId { get; set; }

        public DateTime? LastPlayed { get; set; }

        [MaxLength(100)]
        public string MusicBrainzId { get; set; }

        [MaxLength(65535)]
        public string PartTitles { get; set; }

        public int PlayedCount { get; set; }

        public short Rating { get; set; }

        public string ReleaseMediaId { get; set; }

        [MaxLength(100)]
        public string SpotifyId { get; set; }

        [MaxLength(250)]
        [Required]
        public string Title { get; set; }

        [Required]
        public short TrackNumber { get; set; }
    }
}