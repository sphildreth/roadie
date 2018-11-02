using Microsoft.EntityFrameworkCore;

namespace Roadie.Library.Data
{
    public interface IRoadieDbContext
    {
        DbSet<Artist> Artists { get; set; }
        DbSet<CollectionRelease> CollectionReleases { get; set; }
        DbSet<Collection> Collections { get; set; }
        DbSet<Label> Labels { get; set; }
        DbSet<Playlist> Playlists { get; set; }
        DbSet<ReleaseGenre> ReleaseGenres { get; set; }
        DbSet<ReleaseMedia> ReleaseMedias { get; set; }
        DbSet<Release> Releases { get; set; }
        DbSet<Track> Tracks { get; set; }
    }
}