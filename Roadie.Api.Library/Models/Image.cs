using System;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models
{
    /// <summary>
    /// Image class served to API consumers.
    /// </summary>
    [Serializable]
    public class Image : EntityModelBase
    {
        public byte[] Bytes { get; set; }

        [MaxLength(100)] public string Caption { get; set; }

        [Obsolete("Only here for transition. Will be removed in future release.")]
        public Guid? ArtistId { get; set; }
        [Obsolete("Only here for transition. Will be removed in future release.")]
        public Guid? ReleaseId { get; set; }
        [MaxLength(50)] public string Signature { get; set; }

        [MaxLength(500)] public string ThumbnailUrl { get; set; }

        [MaxLength(500)] public string Url { get; set; }

        public Image()
        {
        }

        /// <summary>
        ///     Set image Url to given value and nullify other entity values, intended to be used in List collection (like images
        ///     for an artist)
        /// </summary>
        public Image(string url) : this(url, null, null)
        {
        }

        public Image(string url, string caption, string thumbnailUrl)
        {
            Url = url;
            ThumbnailUrl = thumbnailUrl;
            CreatedDate = null;
            Id = null;
            Status = null;
            Caption = caption;
        }

        public Image(byte[] bytes)
        {
            Bytes = bytes;
        }
    }
}