using Roadie.Library.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Roadie.Library.Data
{
    public partial class Comment
    {
        [NotMapped]
        public CommentType CommentType
        {
            get
            {
                if(this.ArtistId.HasValue)
                {
                    return CommentType.Artist;
                }
                if(this.CollectionId.HasValue)
                {
                    return CommentType.Collection;
                }
                if(this.GenreId.HasValue)
                {
                    return CommentType.Genre;
                }
                if(this.LabelId.HasValue)
                {
                    return CommentType.Label;
                }
                if(this.PlaylistId.HasValue)
                {
                    return CommentType.Playlist;
                }
                if(this.ReleaseId.HasValue)
                {
                    return CommentType.Release;
                }
                if(this.TrackId.HasValue)
                {
                    return CommentType.Track;
                }
                return CommentType.Unknown;
            }
        }
    }
}
