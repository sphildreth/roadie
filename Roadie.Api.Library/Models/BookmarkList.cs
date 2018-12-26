using Newtonsoft.Json;
using Roadie.Library.Enums;
using Roadie.Library.Models.Collections;
using Roadie.Library.Models.Playlists;
using Roadie.Library.Models.Releases;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models
{
    [Serializable]
    public class BookmarkList : EntityInfoModelBase
    {
        public DataToken User { get; set; }
        public DataToken Bookmark { get; set; }
        public Image Thumbnail { get; set; }
        public BookmarkType? Type { get; set; }
        public string BookmarkType
        {
            get
            {
                return this.Type.ToString(); 
            }
        }
        [JsonIgnore]
        public int BookmarkTargetId { get; set; }
        public string Comment { get; set; }
        public int? Position { get; set; }
        public ArtistList Artist { get; set; }
        public ReleaseList Release { get; set; }
        public TrackList Track { get; set; }
        public PlaylistList Playlist { get; set; }
        public CollectionList Collection { get; set; }
        public LabelList Label { get; set; }
    }
}
