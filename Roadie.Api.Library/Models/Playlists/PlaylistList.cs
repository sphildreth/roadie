using Roadie.Library.Identity;
using Roadie.Library.Utility;
using System;

namespace Roadie.Library.Models.Playlists
{
    [Serializable]
    public class PlaylistList : EntityInfoModelBase
    {
        public decimal? Duration { get; set; }

        public string DurationTime
        {
            get
            {
                if (!Duration.HasValue) return "--:--";
                return new TimeInfo(Duration.Value).ToFullFormattedString();
            }
        }

        public bool IsPublic { get; set; }
        public DataToken Playlist { get; set; }
        public short? PlaylistCount { get; set; }
        public Image Thumbnail { get; set; }
        public short TrackCount { get; set; }
        public DataToken User { get; set; }
        public Image UserThumbnail { get; set; }

        public static PlaylistList FromDataPlaylist(Data.Playlist playlist, ApplicationUser user,
            Image playlistThumbnail, Image userThumbnail)
        {
            return new PlaylistList
            {
                Playlist = new DataToken
                {
                    Text = playlist.Name,
                    Value = playlist.RoadieId.ToString()
                },
                User = new DataToken
                {
                    Text = user.UserName,
                    Value = user.RoadieId.ToString()
                },
                PlaylistCount = playlist.TrackCount,
                IsPublic = playlist.IsPublic,
                Duration = playlist.Duration,
                TrackCount = playlist.TrackCount,
                CreatedDate = playlist.CreatedDate,
                LastUpdated = playlist.LastUpdated,
                UserThumbnail = userThumbnail,
                Id = playlist.RoadieId,
                Thumbnail = playlistThumbnail
            };
        }
    }
}