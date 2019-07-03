using Newtonsoft.Json;
using Roadie.Library.Enums;
using Roadie.Library.Models.Collections;
using Roadie.Library.Models.Playlists;
using Roadie.Library.Models.Releases;
using System;

namespace Roadie.Library.Models
{
    [Serializable]
    public class BookmarkList : EntityInfoModelBase
    {
        public ArtistList Artist { get; set; }
        public DataToken Bookmark { get; set; }
        [JsonIgnore] public int BookmarkTargetId { get; set; }
        public string BookmarkType => Type.ToString();
        public CollectionList Collection { get; set; }
        public string Comment { get; set; }
        public LabelList Label { get; set; }
        public PlaylistList Playlist { get; set; }
        public int? Position { get; set; }
        public ReleaseList Release { get; set; }
        public Image Thumbnail { get; set; }
        public TrackList Track { get; set; }
        public BookmarkType? Type { get; set; }
        public DataToken User { get; set; }
    }
}