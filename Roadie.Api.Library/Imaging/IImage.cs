using Roadie.Library.Enums;
using System;

namespace Roadie.Library.Imaging
{
    public interface IImage
    {
        short SortOrder { get; set; }
        byte[] Bytes { get; set; }
        DateTime CreatedDate { get; set; }
        Guid Id { get; }
        DateTime LastUpdated { get; set; }
        string Signature { get; set; }
        Statuses Status { get; set; }
        string Url { get; set; }
        string CacheKey { get; }
        string CacheRegion { get; }
        string GenerateSignature();
        string ToString();
    }
}