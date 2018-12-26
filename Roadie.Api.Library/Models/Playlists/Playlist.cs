using Mapster;
using Roadie.Library.Models.Statistics;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Playlists
{
    [Serializable]
    public class Playlist : EntityModelBase
    {
        public const string DefaultIncludes = "stats";

        public bool IsPublic { get; set; }
        /// <summary>
        /// If true then a system generated playlist - like top 20 played songs last 30 days
        /// </summary>
        public bool IsSystemGenerated { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public UserList Maintainer { get; set; }
        public IEnumerable<PlaylistTrack> Tracks { get; set; }
        public int? PlaylistCount { get; set; }
        public ReleaseGroupingStatistics Statistics { get; set; }
        public bool UserCanEdit { get; set; }
        public Image Thumbnail { get; set; }
        public Image MediumThumbnail { get; set; }
        public short? TrackCount { get; set; }
        public short? ReleaseCount { get; set; }
        public decimal? Duration { get; set; }
        public string DurationTime
        {
            get
            {
                if (!this.Duration.HasValue)
                {
                    return "--:--";
                }
                return new TimeInfo(this.Duration.Value).ToFullFormattedString();
            }

        }
    }
}
