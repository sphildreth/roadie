using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Roadie.Library.Enums;

namespace Roadie.Library.Models.Releases
{
    /// <summary>
    /// This is used to perform .FromSql() statements and get results from SQL query
    /// </summary>
    [Serializable]
    public class ReleaseListFromSql
    {
        public string ReleaseTitle { get; set; }
        public Guid ReleaseRoadieId { get; set; }
        public Guid ArtistRoadieId { get; set; }
        public string ArtistName { get; set; }
        public short? Rating { get; set; }
        public int? Duration { get; set; }
        public LibraryStatus? LibraryStatus { get; set; }
        public DateTime? ReleaseDateDateTime { get; set; }
        public int? TrackCount { get; set; }
        public int? TrackPlayedCount { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? LastPlayed { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
