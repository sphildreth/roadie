using Roadie.Library.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    public partial class Comment
    {
        [NotMapped]
        public CommentType CommentType
        {
            get
            {
                if (ArtistId.HasValue) return CommentType.Artist;
                if (CollectionId.HasValue) return CommentType.Collection;
                if (GenreId.HasValue) return CommentType.Genre;
                if (LabelId.HasValue) return CommentType.Label;
                if (PlaylistId.HasValue) return CommentType.Playlist;
                if (ReleaseId.HasValue) return CommentType.Release;
                if (TrackId.HasValue) return CommentType.Track;
                return CommentType.Unknown;
            }
        }
    }
}