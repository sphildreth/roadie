using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Roadie.Library.Enums;
using Roadie.Library.Identity;
using Roadie.Library.Models.Releases;
using System;

namespace Roadie.Library.Data
{
    public class RoadieDbContext : DbContext, IRoadieDbContext
    {
        public DbSet<ArtistAssociation> ArtistAssociations { get; set; }
        public DbSet<ArtistGenre> ArtistGenres { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<CollectionRelease> CollectionReleases { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Label> Labels { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistTrack> PlaylistTracks { get; set; }
        public DbSet<ReleaseGenre> ReleaseGenres { get; set; }
        public DbSet<ReleaseLabel> ReleaseLabels { get; set; }
        public DbSet<ReleaseMedia> ReleaseMedias { get; set; }
        public DbSet<Release> Releases { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<ScanHistory> ScanHistories { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<UserArtist> UserArtists { get; set; }
        public DbSet<UserRelease> UserReleases { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<ApplicationRole> UserRoles { get; set; }
        public DbSet<UserTrack> UserTracks { get; set; }
        public DbSet<UserQue> UserQues { get; set; }

        public RoadieDbContext(DbContextOptions<RoadieDbContext> options)
            : base(options)
        {
        }
                          

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //builder
            //    .Entity<Artist>()
            //    .Property(e => e.Status)
            //    .HasConversion(
            //        v => v.ToString(),
            //        v => string.IsNullOrEmpty(v) ? Statuses.Incomplete : (Statuses)Enum.Parse(typeof(Statuses), v))
            //    .HasDefaultValue(Statuses.Incomplete);

            builder
                .Entity<Release>()
                .Property(e => e.ReleaseType)
                .HasConversion(
                    v => v.ToString(),
                    v => string.IsNullOrEmpty(v) ? ReleaseType.Unknown : (ReleaseType)Enum.Parse(typeof(ReleaseType), v))
                .HasDefaultValue(ReleaseType.Release);

            builder
                .Entity<Release>()
                .Property(e => e.LibraryStatus)
                .HasConversion(
                    v => v.ToString(),
                    v => string.IsNullOrEmpty(v) ? LibraryStatus.Incomplete : (LibraryStatus)Enum.Parse(typeof(LibraryStatus), v))
                .HasDefaultValue(LibraryStatus.Incomplete);

            builder
                .Entity<Collection>()
                .Property(e => e.CollectionType)
                .HasConversion(
                    v => v.ToString(),
                    v => string.IsNullOrEmpty(v) ? CollectionType.Unknown : (CollectionType)Enum.Parse(typeof(CollectionType), v))
                .HasDefaultValue(CollectionType.Unknown);

            //builder
            //    .Entity<Bookmark>()
            //    .Property(e => e.BookmarkType)
            //    .HasConversion(
            //        v => v.ToString(),
            //        v => string.IsNullOrEmpty(v) ? BookmarkType.Unknown : (BookmarkType)Enum.Parse(typeof(BookmarkType), v))
            //    .HasDefaultValue(BookmarkType.Unknown);

            builder.Entity<Release>()
                .HasOne(d => d.Artist)
                .WithMany(p => p.Releases)
                .HasForeignKey(d => d.ArtistId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("release_ibfk_1");

            builder.Entity<ReleaseLabel>()
                .HasOne(rl => rl.Release)
                .WithMany(r => r.Labels)
                .HasForeignKey(rl => rl.ReleaseId);

            builder.Entity<ReleaseLabel>()
                .HasOne(rl => rl.Label)
                .WithMany(l => l.ReleaseLabels)
                .HasForeignKey(rl => rl.LabelId);

            builder.Entity<ReleaseMedia>()
                .HasMany(rm => rm.Tracks)
                .WithOne(t => t.ReleaseMedia)
                .HasForeignKey(rm => rm.ReleaseMediaId);

            builder.Entity<ReleaseMedia>()
                .HasOne(rm => rm.Release)
                .WithMany(r => r.Medias)
                .HasForeignKey(r => r.ReleaseId);

            builder.Entity<ReleaseGenre>()
                .HasKey(rg => new { rg.ReleaseId, rg.GenreId });

            builder.Entity<ReleaseGenre>()
                .HasOne(rg => rg.Release)
                .WithMany(r => r.Genres)
                .HasForeignKey(rg => rg.ReleaseId);

            builder.Entity<ReleaseGenre>()
                .HasOne(rg => rg.Genre)
                .WithMany(g => g.Releases)
                .HasForeignKey(rg => rg.GenreId);

            builder.Entity<ArtistGenre>()
                .HasKey(rg => new { rg.ArtistId, rg.GenreId });

            builder.Entity<ArtistGenre>()
                .HasOne(rg => rg.Artist)
                .WithMany(r => r.Genres)
                .HasForeignKey(rg => rg.ArtistId);

            builder.Entity<ArtistGenre>()
                .HasOne(rg => rg.Genre)
                .WithMany(g => g.Artists)
                .HasForeignKey(rg => rg.GenreId);

            builder.Entity<CollectionRelease>()
                .HasOne(cr => cr.Release)
                .WithMany(r => r.Collections)
                .HasForeignKey(cr => cr.ReleaseId);

            builder.Entity<CollectionRelease>()
                .HasOne(cr => cr.Collection)
                .WithMany(c => c.Releases)
                .HasForeignKey(cr => cr.CollectionId);

            builder.Entity<Bookmark>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookmarks)
                .HasForeignKey(b => b.UserId);

            builder.Entity<ArtistAssociation>()
                .HasOne(aa => aa.Artist)
                .WithMany(a => a.AssociatedArtists)
                .HasForeignKey(aa => aa.AssociatedArtistId);

            //builder.Entity<Track>()
            //    .HasOne(t => t.TrackArtist)
            //    .WithMany(a => a.Tracks)
            //    .HasForeignKey(t => t.ArtistId);


            //// I dont understand why the "1" but without this self join generates "1" based columns on the selectd and blows up ef.
            //builder.Entity<Artist>()
            //    .HasMany(e => e.AssociatedArtists)
            //    .WithOne(e => e.Artist)
            //    .HasForeignKey(e => e.ArtistId);

            //builder.Entity<Artist>()
            //    .HasMany(e => e.AssociatedArtists1)
            //    .WithOne(e => e.AssociatedArtist)
            //    .HasForeignKey(e => e.AssociatedArtistId);
        }
    }
}