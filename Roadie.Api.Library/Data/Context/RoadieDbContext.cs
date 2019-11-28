using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Roadie.Library.Enums;
using Roadie.Library.Identity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roadie.Library.Data.Context
{
    public abstract partial class RoadieDbContext : DbContext, IRoadieDbContext
    {
        public DbSet<ArtistAssociation> ArtistAssociations { get; set; }

        public DbSet<ArtistGenre> ArtistGenres { get; set; }

        public DbSet<Artist> Artists { get; set; }

        public DbSet<ArtistSimilar> ArtistSimilar { get; set; }

        public DbSet<Bookmark> Bookmarks { get; set; }

        public DbSet<ChatMessage> ChatMessages { get; set; }

        public DbSet<CollectionMissing> CollectionMissings { get; set; }

        public DbSet<CollectionRelease> CollectionReleases { get; set; }

        public DbSet<Collection> Collections { get; set; }

        public DbSet<CommentReaction> CommentReactions { get; set; }

        public DbSet<Comment> Comments { get; set; }

        public DbSet<Credit> Credits { get; set; }
        public DbSet<CreditCategory> CreditCategory { get; set; }

        public DbSet<Genre> Genres { get; set; }

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

        public DbSet<UserQue> UserQues { get; set; }

        public DbSet<UserRelease> UserReleases { get; set; }

        public DbSet<UserRole> UserRoles { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<UserTrack> UserTracks { get; set; }
        public DbSet<InviteToken> InviteTokens { get; set; }

        public RoadieDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
       //     base.OnModelCreating(builder);

            builder.Entity<Artist>(entity =>
            {
                entity
                    .Property(e => e.BandStatus)
                    .HasConversion(
                        v => v.ToString(),
                        v => string.IsNullOrEmpty(v) ? BandStatus.Unknown : (BandStatus)Enum.Parse(typeof(BandStatus), v))
                    .HasDefaultValue(BandStatus.Unknown);

                entity.HasIndex(e => e.Name)
                    .HasName("ix_artist_name")
                    .IsUnique();

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_artist_roadieId");

                entity.HasIndex(e => e.SortName)
                    .HasName("ix_artist_sortname")
                    .IsUnique();
            });

            builder.Entity<ArtistAssociation>(entity =>
            {
                entity.HasIndex(e => e.AssociatedArtistId)
                    .HasName("ix_associatedArtistId");

                entity.HasIndex(e => new { e.ArtistId, e.AssociatedArtistId })
                    .HasName("ix__artistAssociation");

                entity.HasOne(d => d.Artist)
                    .WithMany(p => p.AssociatedArtists)
                    .HasForeignKey(d => d.ArtistId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("artistAssociation_ibfk_1");
            });

            builder.Entity<ArtistGenre>(entity =>
            {
                entity.HasIndex(e => e.ArtistId)
                    .HasName("ix_artistGenreTable_artistId");

                entity.HasIndex(e => e.GenreId)
                    .HasName("ix_artistGenre_genreId");

                entity.HasIndex(e => new { e.ArtistId, e.GenreId })
                    .HasName("ix__artistGenreAssociation");

                entity.HasOne(d => d.Artist)
                    .WithMany(p => p.Genres)
                    .HasForeignKey(d => d.ArtistId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("artistGenreTable_ibfk_1");

                entity.HasOne(d => d.Genre)
                    .WithMany(p => p.Artists)
                    .HasForeignKey(d => d.GenreId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("artistGenreTable_ibfk_2");
            });

            builder.Entity<ArtistSimilar>(entity =>
            {
                entity.HasIndex(e => e.SimilarArtistId)
                    .HasName("ix_similarArtistId");

                entity.HasIndex(e => new { e.ArtistId, e.SimilarArtistId })
                    .HasName("ix_artistSimilar");

                entity.HasOne(d => d.Artist)
                    .WithMany(p => p.SimilarArtists)
                    .HasForeignKey(d => d.ArtistId)
                    .HasConstraintName("artistSimilar_ibfk_1");
            });

            builder.Entity<Bookmark>(entity =>
            {
                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_bookmark_roadieId");

                entity.HasIndex(e => e.UserId)
                    .HasName("ix_bookmark_userId");

                entity.HasIndex(e => new { e.BookmarkType, e.BookmarkTargetId, e.UserId })
                    .HasName("ix_bookmark_bookmarkType")
                    .IsUnique();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Bookmarks)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("bookmark_ibfk_1");
            });

            builder.Entity<ChatMessage>(entity =>
            {
                entity.HasIndex(e => e.UserId)
                    .HasName("ix__chatMessage_user");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ChatMessages)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("chatMessage_ibfk_1");
            });

            builder.Entity<Collection>(entity =>
            {
                entity
                    .Property(e => e.CollectionType)
                    .HasConversion(
                        v => v.ToString(),
                        v => string.IsNullOrEmpty(v)
                            ? CollectionType.Unknown
                            : (CollectionType)Enum.Parse(typeof(CollectionType), v))
                    .HasDefaultValue(CollectionType.Unknown);

                entity.HasIndex(e => e.MaintainerId)
                    .HasName("ix_collection_maintainerId");

                entity.HasIndex(e => e.Name)
                    .HasName("ix_collection_name")
                    .IsUnique();

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_collection_roadieId");

                entity.HasOne(d => d.Maintainer)
                    .WithMany(p => p.Collections)
                    .HasForeignKey(d => d.MaintainerId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("collection_ibfk_1");
            });

            builder.Entity<CollectionMissing>(entity =>
            {
                entity.HasIndex(e => e.CollectionId)
                    .HasName("ix_collection_collectionId");

                entity.HasOne(d => d.Collection)
                    .WithMany(p => p.MissingReleases)
                    .HasForeignKey(d => d.CollectionId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("collection_missing_ibfk_1");
            });

            builder.Entity<CollectionRelease>(entity =>
            {
                entity.HasIndex(e => e.ReleaseId)
                    .HasName("ix_collectionrelease_releaseId");

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_collectionrelease_roadieId");

                entity.HasIndex(e => new { e.CollectionId, e.ReleaseId })
                    .HasName("ix__collection_release");

                entity.HasOne(d => d.Collection)
                    .WithMany(p => p.Releases)
                    .HasForeignKey(d => d.CollectionId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("collectionrelease_ibfk_2");

                entity.HasOne(d => d.Release)
                    .WithMany(p => p.Collections)
                    .HasForeignKey(d => d.ReleaseId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("collectionrelease_ibfk_1");
            });

            builder.Entity<Comment>(entity =>
            {
                entity.HasIndex(e => e.ArtistId)
                    .HasName("ix_commentartist_ibfk_1");

                entity.HasIndex(e => e.CollectionId)
                    .HasName("ix_commentcollection_ibfk_1");

                entity.HasIndex(e => e.GenreId)
                    .HasName("ix_commentgenre_ibfk_1");

                entity.HasIndex(e => e.LabelId)
                    .HasName("ix_commentlabel_ibfk_1");

                entity.HasIndex(e => e.PlaylistId)
                    .HasName("ix_commentplaylist_ibfk_1");

                entity.HasIndex(e => e.ReleaseId)
                    .HasName("ix_commentrelease_ibfk_1");

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_comment_roadieId");

                entity.HasIndex(e => e.TrackId)
                    .HasName("ix_commenttrack_ibfk_1");

                entity.HasIndex(e => e.UserId)
                    .HasName("ix_commentuser_ibfk_1");

                entity.HasOne(d => d.Artist)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.ArtistId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("commentartist_ibfk_1");

                entity.HasOne(d => d.Collection)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.CollectionId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("commentcollection_ibfk_1");

                entity.HasOne(d => d.Genre)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.GenreId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("commentgenre_ibfk_1");

                entity.HasOne(d => d.Label)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.LabelId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("commentlabel_ibfk_1");

                entity.HasOne(d => d.Playlist)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.PlaylistId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("commentplaylist_ibfk_1");

                entity.HasOne(d => d.Release)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.ReleaseId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("commentrelease_ibfk_1");

                entity.HasOne(d => d.Track)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.TrackId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("commenttrack_ibfk_1");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("commentuser_ibfk_1");
            });

            builder.Entity<CommentReaction>(entity =>
            {
                entity.HasIndex(e => e.CommentId)
                    .HasName("ix_commentReactioncomment_ibfk_1");

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_commentReaction_roadieId");

                entity.HasIndex(e => e.UserId)
                    .HasName("ix_commentReactionuser_ibfk_1");

                entity.HasIndex(e => new { e.UserId, e.CommentId })
                    .HasName("ix_commentReaction_userId")
                    .IsUnique();

                entity.HasOne(d => d.Comment)
                    .WithMany(p => p.Reactions)
                    .HasForeignKey(d => d.CommentId)
                    .HasConstraintName("commentReactioncomment_ibfk_1");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CommentReactions)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("commentReactionuser_ibfk_1");
            });

            builder.Entity<Credit>(entity =>
            {
                entity.HasIndex(e => e.ArtistId)
                    .HasName("ix_credit_artist_ibfk_1");

                entity.HasIndex(e => e.CreditCategoryId)
                    .HasName("ix_credit_category_ibfk_1");

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_credit_roadieId");

                entity.HasIndex(e => new { e.ReleaseId, e.Id })
                    .HasName("ix_creditCreditandRelease");

                entity.HasIndex(e => new { e.TrackId, e.Id })
                    .HasName("ix_creditCreditandTrack");

                entity.HasOne(d => d.Artist)
                    .WithMany(p => p.Credits)
                    .HasForeignKey(d => d.ArtistId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("credit_artist_ibfk_1");

                entity.HasOne(d => d.CreditCategory)
                    .WithMany(p => p.Credits)
                    .HasForeignKey(d => d.CreditCategoryId)
                    .HasConstraintName("credit_category_ibfk_1");

                entity.HasOne(d => d.Release)
                    .WithMany(p => p.Credits)
                    .HasForeignKey(d => d.ReleaseId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("credit_release_ibfk_1");

                entity.HasOne(d => d.Track)
                    .WithMany(p => p.Credits)
                    .HasForeignKey(d => d.TrackId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("credit_track_ibfk_1");
            });

            builder.Entity<CreditCategory>(entity =>
            {
                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_creditCategory_roadieId");
            });

            builder.Entity<Genre>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .HasName("ix_genre_name")
                    .IsUnique();

                entity.HasIndex(e => e.NormalizedName)
                    .HasName("ix_genre_normalizedName");

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_genre_roadieId");
            });

            builder.Entity<InviteToken>(entity =>
            {
                entity.HasIndex(e => e.CreatedByUserId)
                    .HasName("inviteToken_fk_1");

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_inviteToken_roadieId");

                entity.HasOne(d => d.CreatedByUser)
                    .WithMany(p => p.InviteTokens)
                    .HasForeignKey(d => d.CreatedByUserId)
                    .HasConstraintName("inviteToken_fk_1");
            });

            builder.Entity<Label>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .HasName("ix_label_name")
                    .IsUnique();

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_label_roadieId");
            });

            builder.Entity<Playlist>(entity =>
            {
                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_playlist_roadieId");

                entity.HasIndex(e => e.UserId)
                    .HasName("ix_playlist_userId");

                entity.HasIndex(e => new { e.Name, e.UserId })
                    .HasName("ix_playlist_name")
                    .IsUnique();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Playlists)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("playlist_ibfk_1");
            });

            builder.Entity<PlaylistTrack>(entity =>
            {
                entity.HasIndex(e => e.PlayListId)
                    .HasName("ix_playListId");

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_playlisttrack_roadieId");

                entity.HasIndex(e => e.TrackId)
                    .HasName("trackId");

                entity.HasOne(d => d.Playlist)
                    .WithMany(p => p.Tracks)
                    .HasForeignKey(d => d.PlayListId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("playlisttrack_ibfk_2");

                entity.HasOne(d => d.Track)
                    .WithMany(p => p.Playlists)
                    .HasForeignKey(d => d.TrackId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("playlisttrack_ibfk_1");
            });

            builder.Entity<Release>(entity =>
            {
                entity
                    .Property(e => e.ReleaseType)
                    .HasConversion(
                        v => v.ToString(),
                        v => string.IsNullOrEmpty(v)
                            ? ReleaseType.Unknown
                            : (ReleaseType)Enum.Parse(typeof(ReleaseType), v))
                    .HasDefaultValue(ReleaseType.Release);

                entity
                    .Property(e => e.LibraryStatus)
                    .HasConversion(
                        v => v.ToString(),
                        v => string.IsNullOrEmpty(v)
                            ? LibraryStatus.Incomplete
                            : (LibraryStatus)Enum.Parse(typeof(LibraryStatus), v))
                    .HasDefaultValue(LibraryStatus.Incomplete);

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_release_roadieId");

                entity.HasIndex(e => e.Title)
                    .HasName("ix_release_title");

                entity.HasIndex(e => new { e.ArtistId, e.Title })
                    .HasName("ix_releaseArtistAndTitle")
                    .IsUnique();

                entity.HasOne(d => d.Artist)
                    .WithMany(p => p.Releases)
                    .HasForeignKey(d => d.ArtistId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("release_ibfk_1");
            });

            builder.Entity<ReleaseGenre>(entity =>
            {
                entity.HasIndex(e => e.GenreId)
                    .HasName("ix_releaseGenre_genreId");

                entity.HasIndex(e => new { e.ReleaseId, e.GenreId })
                    .HasName("ix_releaseGenreTableReleaseAndGenre");

                entity.HasOne(d => d.Genre)
                    .WithMany(p => p.Releases)
                    .HasForeignKey(d => d.GenreId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("releaseGenreTable_ibfk_2");

                entity.HasOne(d => d.Release)
                    .WithMany(p => p.Genres)
                    .HasForeignKey(d => d.ReleaseId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("releaseGenreTable_ibfk_1");
            });

            builder.Entity<ReleaseLabel>(entity =>
            {
                entity.HasIndex(e => e.LabelId)
                    .HasName("ix_releaselabel_labelId");

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_releaselabel_roadieId");

                entity.HasIndex(e => new { e.ReleaseId, e.LabelId })
                    .HasName("ix_release_label");

                entity.HasOne(d => d.Label)
                    .WithMany(p => p.ReleaseLabels)
                    .HasForeignKey(d => d.LabelId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("releaselabel_ibfk_2");

                entity.HasOne(d => d.Release)
                    .WithMany(p => p.Labels)
                    .HasForeignKey(d => d.ReleaseId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("releaselabel_ibfk_1");
            });

            builder.Entity<ReleaseMedia>(entity =>
            {
                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_releasemedia_roadieId");

                entity.HasIndex(e => new { e.ReleaseId, e.MediaNumber })
                    .HasName("ix_releasemedia_releaseId");

                entity.HasOne(d => d.Release)
                    .WithMany(p => p.Medias)
                    .HasForeignKey(d => d.ReleaseId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("releasemedia_ibfk_1");
            });

            builder.Entity<Request>(entity =>
            {
                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_request_roadieId");

                entity.HasIndex(e => e.UserId)
                    .HasName("ix_requestartist_ibfk_1");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Requests)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("requestartist_ibfk_1");
            });

            builder.Entity<ScanHistory>(entity =>
            {
                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_scanHistory_roadieId");

                entity.HasIndex(e => e.UserId)
                    .HasName("ix_rscanHistoryt_ibfk_1");
            });

            builder.Entity<Submission>(entity =>
            {
                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_submission_roadieId");

                entity.HasIndex(e => e.UserId)
                    .HasName("ix_submission_ibfk_1");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Submissions)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("submission_ibfk_1");
            });

            builder.Entity<Track>(entity =>
            {
                entity.HasIndex(e => e.ArtistId)
                    .HasName("ix_track_artistId");

                entity.HasIndex(e => e.Hash)
                    .HasName("ix_track_hash")
                    .IsUnique();

                entity.HasIndex(e => e.ReleaseMediaId)
                    .HasName("ix_track_releaseMediaId");

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_track_roadieId");

                entity.HasIndex(e => e.Title)
                    .HasName("ix_track_title");

                entity.HasIndex(e => new { e.ReleaseMediaId, e.TrackNumber })
                    .HasName("ix_track_unique_to_eleasemedia")
                    .IsUnique();

                entity.HasOne(d => d.TrackArtist)
                    .WithMany(p => p.Tracks)
                    .HasForeignKey(d => d.ArtistId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("track_artist_ibfk_1");

                entity.HasOne(d => d.ReleaseMedia)
                    .WithMany(p => p.Tracks)
                    .HasForeignKey(d => d.ReleaseMediaId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("track_ibfk_1");
            });

            builder.Entity<UserQue>(entity =>
            {
                entity.HasIndex(e => e.TrackId)
                    .HasName("ix_userQue_ibfk_2");

                entity.HasIndex(e => e.UserId)
                    .HasName("ix_user");

                entity.HasOne(d => d.Track)
                    .WithMany(p => p.UserQues)
                    .HasForeignKey(d => d.TrackId)
                    .HasConstraintName("userQue_ibfk_2");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserQues)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("userQue_ibfk_1");
            });

            builder.Entity<UserArtist>(entity =>
            {
                entity.HasIndex(e => e.ArtistId)
                    .HasName("ix_userartist_artistId");

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_userartist_roadieId");

                entity.HasIndex(e => new { e.UserId, e.ArtistId })
                    .HasName("ix_userartist_userId")
                    .IsUnique();

                entity.HasOne(d => d.Artist)
                    .WithMany(p => p.UserArtists)
                    .HasForeignKey(d => d.ArtistId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("userartist_ibfk_2");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ArtistRatings)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("userartist_ibfk_1");
            });

            builder.Entity<UserRelease>(entity =>
            {
                entity.HasIndex(e => e.ReleaseId)
                    .HasName("ix_userrelease_releaseId");

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_userrelease_roadieId");

                entity.HasIndex(e => new { e.UserId, e.ReleaseId })
                    .HasName("ix_userrelease_userId_ix")
                    .IsUnique();

                entity.HasOne(d => d.Release)
                    .WithMany(p => p.UserRelases)
                    .HasForeignKey(d => d.ReleaseId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("userrelease_ibfk_2");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserReleases)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("userrelease_ibfk_1");
            });

            builder.Entity<UserTrack>(entity =>
            {
                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_usertrack_roadieId");

                entity.HasIndex(e => e.TrackId)
                    .HasName("ix_usertrack_trackId");

                entity.HasIndex(e => new { e.UserId, e.TrackId })
                    .HasName("ix_usertrack_userId_ix")
                    .IsUnique();

                entity.HasOne(d => d.Track)
                    .WithMany(p => p.UserTracks)
                    .HasForeignKey(d => d.TrackId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("usertrack_ibfk_2");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserTracks)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("usertrack_ibfk_1");
            });

            builder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email)
                    .HasName("ix_user_email")
                    .IsUnique();

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_user_roadieId");

                entity.HasIndex(e => e.UserName)
                    .HasName("ix_user_username")
                    .IsUnique();
            });

            builder.Entity<UserClaims>(entity =>
            {
                entity.HasIndex(e => e.UserId)
                    .HasName("ix_userClaims_userId");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserClaims)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("userClaims_ibfk_1");
            });

            builder.Entity<UserRoleClaims>(entity =>
            {
                entity.HasIndex(e => e.RoleId)
                    .HasName("ix_userRoleClaims_userRoleId");

                entity.HasOne(d => d.UserRole)
                    .WithMany(p => p.RoleClaims)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("userRoleClaims_ibfk_1");
            });

            builder.Entity<UserRole>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .HasName("ix_userrole_name")
                    .IsUnique();

                entity.HasIndex(e => e.RoadieId)
                    .HasName("ix_userrole_roadieId");
            });

            builder.Entity<UsersInRoles>(entity =>
            {
                entity.HasIndex(e => e.UserId)
                    .HasName("ix_usersInRoles_userId");

                entity.HasIndex(e => e.RoleId)
                    .HasName("ix_usersInRoles_userRoleId");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserRoles)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("usersInRoles_ibfk_1");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.UserRoles)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("usersInRoles_ibfk_2");
            });
        }

        Task<EntityEntry> IRoadieDbContext.AddAsync(object entity, CancellationToken cancellationToken) => throw new NotImplementedException();

        Task<EntityEntry<TEntity>> IRoadieDbContext.AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) => throw new NotImplementedException();

        Task<TEntity> IRoadieDbContext.FindAsync<TEntity>(params object[] keyValues) => throw new NotImplementedException();

        Task<object> IRoadieDbContext.FindAsync(Type entityType, object[] keyValues, CancellationToken cancellationToken) => throw new NotImplementedException();

        Task<TEntity> IRoadieDbContext.FindAsync<TEntity>(object[] keyValues, CancellationToken cancellationToken) => throw new NotImplementedException();

        Task<object> IRoadieDbContext.FindAsync(Type entityType, params object[] keyValues) => throw new NotImplementedException();
    }
}