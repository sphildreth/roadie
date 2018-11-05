using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Roadie.Data.Models
{
    [Serializable]
    public class Artist : EntityModelBase
    {
        [MaxLength(100)]
        public string AmgId { get; set; }

        public string ArtistType { get; set; }

        public string BandStatus { get; set; }

        [MaxLength(65535)]
        public string BioContext { get; set; }

        public DateTime? BirthDate { get; set; }

        [MaxLength(50)]
        public string DiscogsId { get; set; }

        [MaxLength(65535)]
        [JsonIgnore]
        [IgnoreDataMember]
        public string ISNIList { get; set; }

        [JsonProperty("isniList")]
        public IEnumerable<string> ISNIListList
        {
            get
            {
                if (string.IsNullOrEmpty(this.ISNIList))
                {
                    return null;
                }
                return this.ISNIList.Split('|');
            }
        }

        [MaxLength(100)]
        public string ITunesId { get; set; }

        [MaxLength(100)]
        public string MusicBrainzId { get; set; }

        [MaxLength(250)]
        public string Name { get; set; }

        [MaxLength(65535)]
        public string Profile { get; set; }

        public short? Rating { get; set; }

        [MaxLength(500)]
        public string RealName { get; set; }

        [MaxLength(100)]
        public string SpotifyId { get; set; }

        public string Tooltip
        {
            get
            {
                return this.Name;
            }
        }

        public List<AssociatedArtist> AssociatedArtists { get; set; }

        public Artist()
        {
            this.BandStatus = "1";
        }
    }
}