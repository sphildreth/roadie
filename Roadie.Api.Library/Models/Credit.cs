using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Roadie.Library.Models
{
    [Serializable]
    public class Credit : EntityModelBase
    {
        public int CreditCategoryId { get; set; }

        [MaxLength(4000)]
        public string Description { get; set; }

        /// <summary>
        /// Full Name when not an Artist via ArtistId (like a Producer)
        /// </summary>
        [MaxLength(500)]
        public string CreditToName { get; set; }

        public int ArtistId { get; set; }

        public int ReleaseId { get; set; }

        public int TrackId { get; set; }
    }
}
