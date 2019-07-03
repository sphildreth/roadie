using Roadie.Library.Enums;
using System;

namespace Roadie.Library.Models.Releases
{
    /// <summary>
    ///     This is used to perform .FromSql() statements and get results from SQL query
    /// </summary>
    [Serializable]
    public class ReleaseListFromSql
    {
        public string ArtistName { get; set; }
        public Guid ArtistRoadieId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? Duration { get; set; }
        public DateTime? LastPlayed { get; set; }
        public DateTime? LastUpdated { get; set; }
        public LibraryStatus? LibraryStatus { get; set; }
        public short? Rating { get; set; }
        public DateTime? ReleaseDateDateTime { get; set; }
        public Guid ReleaseRoadieId { get; set; }
        public string ReleaseTitle { get; set; }
        public int? TrackCount { get; set; }
        public int? TrackPlayedCount { get; set; }
    }
}