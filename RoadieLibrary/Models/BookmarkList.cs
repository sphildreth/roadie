using Roadie.Library.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models
{
    [Serializable]
    public class BookmarkList : EntityInfoModelBase
    {
        public DataToken Bookmark { get; set; }
        public string ThumbnailUrl { get; set; }
        public BookmarkType Type { get; set; }
    }
}
