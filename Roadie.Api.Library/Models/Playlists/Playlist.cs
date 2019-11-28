using Roadie.Library.Models.Statistics;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;

namespace Roadie.Library.Models.Playlists
{
    [Serializable]
    public class Playlist : EntityModelBase
    {
        public const string DefaultIncludes = "comments,stats";

        public IEnumerable<Comment> Comments { get; set; }
        public string Description { get; set; }
        public decimal? Duration { get; set; }

        public string DurationTime
        {
            get
            {
                if (!Duration.HasValue)
                {
                    return "--:--";
                }
                return new TimeInfo(Duration.Value).ToFullFormattedString();
            }
        }

        public bool IsPublic { get; set; }

        /// <summary>
        ///     If true then a system generated playlist - like top 20 played songs last 30 days
        /// </summary>
        public bool IsSystemGenerated { get; set; }

        public UserList Maintainer { get; set; }
        public Image MediumThumbnail { get; set; }
        public string Name { get; set; }

        // When populated a "data:image" base64 byte array of an image to use as new Thumbnail
        public string NewThumbnailData { get; set; }

        public int? PlaylistCount { get; set; }
        public short? ReleaseCount { get; set; }
        public ReleaseGroupingStatistics Statistics { get; set; }
        public Image Thumbnail { get; set; }
        public short? TrackCount { get; set; }
        public IEnumerable<PlaylistTrack> Tracks { get; set; }
        public bool UserCanEdit { get; set; }
    }
}