using System;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models
{
    [Serializable]
    public class Image : EntityModelBase
    {
        public Guid? ArtistId { get; set; }


        public byte[] Bytes { get; set; }

        [MaxLength(100)]
        public string Caption { get; set; }

        public Guid? ReleaseId { get; set; }

        [MaxLength(50)]
        public string Signature { get; set; }

        [MaxLength(500)]
        public string Url { get; set; }

        public Image()
        {
        }

        /// <summary>
        /// Set image Url to given value and nullify other entity values, intended to be used in List collection (like images for an artist)
        /// </summary>
        public Image(string url)
        {
            this.Url = url;
            this.CreatedDate = null;
            this.Id = null;
            this.Status = null;
        }

        public Image(byte[] bytes)
        {
            this.Bytes = bytes;
        }
    }
}