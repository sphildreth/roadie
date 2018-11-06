using Roadie.Library.Models.Statistics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Playlists
{
    [Serializable]
    public class Playlist
    {
        public bool IsPublic { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid UserId { get; set; }
        public IEnumerable<PlaylistTrack> Tracks { get; set; }
        public IEnumerable<string> Urls { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public int? PlaylistCount { get; set; }
        public string ThumbnailUrl { get; set; }
        public ReleaseGroupingStatistics Stats { get; set; }
        public string PlaylistPlayUrl { get; set; }
        public bool UserCanEdit { get; set; }
        public string UrlsTokenfield { get; set; }
        public string TagsTokenfield { get; set; }
        public Image Thumbnail { get; set; }
        public string BookmarkRoadieId { get; internal set; }
    }
}
