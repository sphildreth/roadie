using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Roadie.Library.Identity;
using Roadie.Library.Models.Releases;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Roadie.Library.Data
{
    public interface IRoadieDbContext : IDisposable, IInfrastructure<IServiceProvider>, IDbContextDependencies, IDbSetCache, IDbQueryCache, IDbContextPoolable
    {
        DbSet<ArtistAssociation> ArtistAssociations { get; set; }
        DbSet<ArtistGenre> ArtistGenres { get; set; }
        DbSet<Artist> Artists { get; set; }
        DbSet<Bookmark> Bookmarks { get; set; }
        ChangeTracker ChangeTracker { get; }
        DbSet<ChatMessage> ChatMessages { get; set; }
        DbSet<CollectionRelease> CollectionReleases { get; set; }
        DbSet<Collection> Collections { get; set; }
        DatabaseFacade Database { get; }
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
        DbSet<UserQue> UserQues { get; set; }
        DbSet<UserRelease> UserReleases { get; set; }
        DbSet<ApplicationUser> Users { get; set; }
        DbSet<UserTrack> UserTracks { get; set; }

        EntityEntry Add(object entity);

        EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;

        Task<EntityEntry> AddAsync(object entity, CancellationToken cancellationToken = default(CancellationToken));

        Task<EntityEntry<TEntity>> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default(CancellationToken)) where TEntity : class;

        void AddRange(IEnumerable<object> entities);

        void AddRange(params object[] entities);

        Task AddRangeAsync(IEnumerable<object> entities, CancellationToken cancellationToken = default(CancellationToken));

        Task AddRangeAsync(params object[] entities);

        EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class;

        EntityEntry Attach(object entity);

        void AttachRange(params object[] entities);

        void AttachRange(IEnumerable<object> entities);

        EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

        EntityEntry Entry(object entity);

        bool Equals(object obj);

        object Find(Type entityType, params object[] keyValues);

        TEntity Find<TEntity>(params object[] keyValues) where TEntity : class;

        Task<TEntity> FindAsync<TEntity>(params object[] keyValues) where TEntity : class;

        Task<object> FindAsync(Type entityType, object[] keyValues, CancellationToken cancellationToken);

        Task<TEntity> FindAsync<TEntity>(object[] keyValues, CancellationToken cancellationToken) where TEntity : class;

        Task<object> FindAsync(Type entityType, params object[] keyValues);

        int GetHashCode();

        DbQuery<TQuery> Query<TQuery>() where TQuery : class;

        EntityEntry Remove(object entity);

        EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class;

        void RemoveRange(IEnumerable<object> entities);

        void RemoveRange(params object[] entities);

        int SaveChanges(bool acceptAllChangesOnSuccess);

        int SaveChanges();

        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken));

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));

        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        string ToString();

        EntityEntry Update(object entity);

        EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;

        void UpdateRange(params object[] entities);

        void UpdateRange(IEnumerable<object> entities);
    }
}