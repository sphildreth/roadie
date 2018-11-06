using System;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models
{
    [Serializable]
    public class Label : EntityModelBase
    {
        [MaxLength(65535)]
        public string BioContext { get; set; }

        public DateTime? BirthDate { get; set; }

        [MaxLength(50)]
        public string DiscogsId { get; set; }

        [MaxLength(100)]
        public string MusicBrainzId { get; set; }

        [MaxLength(250)]
        public string Name { get; set; }

        [MaxLength(65535)]
        public string Profile { get; set; }

        public Image Thumbnail { get; set; }
    }
}