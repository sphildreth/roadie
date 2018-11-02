using Microsoft.EntityFrameworkCore;
using Roadie.Library.Identity;

namespace Roadie.Library.Data
{
    public interface IRoadieDbContext
    {
        DbSet<ArtistAssociation> ArtistAssociations { get; set; }
        DbSet<ArtistGenre> ArtistGenres { get; set; }
        DbSet<Artist> Artists { get; set; }
        DbSet<Bookmark> Bookmarks { get; set; }
        DbSet<CollectionRelease> CollectionReleases { get; set; }
        DbSet<Collection> Collections { get; set; }
        DbSet<Genre> Genres { get; set; }
        DbSet<Image> Images { get; set; }
        DbSet<Label> Labels { get; set; }
        DbSet<Playlist> Playlists { get; set; }
        DbSet<PlaylistTrack> PlaylistTracks { get; set; }
        DbSet<ReleaseGenre> ReleaseGenres { get; set; }
        DbSet<ReleaseLabel> ReleaseLabels { get; set; }
        DbSet<ReleaseMedia> ReleaseMedias { get; set; }
        DbSet<Release> Releases { get; set; }
        DbSet<Request> Requests { get; set; }
        DbSet<Submission> Submissions { get; set; }
        DbSet<Track> Tracks { get; set; }
        DbSet<UserArtist> UserArtists { get; set; }
        DbSet<UserRelease> UserReleases { get; set; }
        DbSet<ApplicationUser> Users { get; set; }
        DbSet<UserTrack> UserTracks { get; set; }
    }
}