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
        public Image Thumbnail { get; set; }
        public BookmarkType Type { get; set; }
    }
}
