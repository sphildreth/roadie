using Microsoft.EntityFrameworkCore;
using Roadie.Library.Enums;
using System;

namespace Roadie.Library.Data
{
    public class RoadieDbContext : DbContext, IRoadieDbContext
    {
        public DbSet<Artist> Artists { get; set; }
        public DbSet<CollectionRelease> CollectionReleases { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<Label> Labels { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<ReleaseGenre> ReleaseGenres { get; set; }
        public DbSet<ReleaseMedia> ReleaseMedias { get; set; }
        public DbSet<Release> Releases { get; set; }
        public DbSet<Track> Tracks { get; set; }

        public RoadieDbContext(DbContextOptions<RoadieDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder
                .Entity<Release>()
                .Property(e => e.ReleaseType)
                .HasConversion(
                    v => v.ToString(),
                    v => string.IsNullOrEmpty(v) ? ReleaseType.Unknown : (ReleaseType)Enum.Parse(typeof(ReleaseType), v))
                .HasDefaultValue(ReleaseType.Unknown);

            builder
                .Entity<Release>()
                .Property(e => e.LibraryStatus)
                .HasConversion(
                    v => v.ToString(),
                    v => (LibraryStatus)Enum.Parse(typeof(LibraryStatus), v))
                .HasDefaultValue(LibraryStatus.Incomplete);

            builder
                .Entity<Collection>()
                .Property(e => e.CollectionType)
                .HasConversion(
                    v => v.ToString(),
                    v => (CollectionType)Enum.Parse(typeof(CollectionType), v))
                .HasDefaultValue(CollectionType.Unknown);

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

            builder.Entity<CollectionRelease>()
                .HasOne(cr => cr.Release)
                .WithMany(r => r.Collections)
                .HasForeignKey(cr => cr.ReleaseId);

            builder.Entity<CollectionRelease>()
                .HasOne(cr => cr.Collection)
                .WithMany(c => c.Releases)
                .HasForeignKey(cr => cr.CollectionId);
        }
    }
}