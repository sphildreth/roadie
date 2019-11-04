using Roadie.Library.Enums;
using System;
using System.Linq;

namespace Roadie.Library.Imaging
{
    /// <summary>
    /// Image class used internally.
    /// </summary>
    [Serializable]
    public class Image : IImage
    {
        public Guid Id { get; }
        public Statuses Status { get; set; }
        public short SortOrder { get; set; }
        public byte[] Bytes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Signature { get; set; }
        public string Url { get; set; }

        public string GenerateSignature()
        {
            if (Bytes == null || !Bytes.Any())
            {
                return null;
            }
            return ImageHasher.AverageHash(Bytes).ToString();
        }

        public Image()
        {
        }

        public Image(Guid id)
        {
            Id = id;
        }

        public override string ToString() => $"Id [{Id}]";

        public string CacheKey => CacheUrn(Id);

        public string CacheRegion => CacheRegionUrn(Id);

        public static string CacheRegionUrn(Guid Id)
        {
            return $"urn:artist:{Id}";
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:artist_by_id:{Id}";
        }
    }
}