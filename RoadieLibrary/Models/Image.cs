using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Roadie.Data.Models
{
    [Serializable]
    public class Image  : EntityModelBase
    {
        public int? ArtistId { get; set; }

        public byte[] Bytes { get; set; }

        [MaxLength(100)]
        public string Caption { get; set; }

        public int? ReleaseId { get; set; }

        [MaxLength(50)]
        public string Signature { get; set; }

        [MaxLength(500)]
        public string Url { get; set; }
    }
}
