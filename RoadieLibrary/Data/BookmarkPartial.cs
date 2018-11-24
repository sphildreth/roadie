using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Data
{
    public partial class Bookmark
    {
        public override string ToString()
        {
            return $"Id [{ this.Id }], BookmarkType [{ this.BookmarkType }], BookmarkTargetId [{ this.BookmarkTargetId }]";
        }
    }
}
