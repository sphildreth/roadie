using Microsoft.AspNetCore.Identity;
using Roadie.Library.Data;
using Roadie.Library.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Identity
{
    /// <summary>
    ///     Application User for Identity
    ///     <remarks>
    ///         As this is used by UserManager to get for each request in API *do not* lazy load properties as the object
    ///         is too heavy and requires multiple DB hits to poplate - which is data not needed to authenticate a user.
    ///     </remarks>
    /// </summary>
    [Table("user")]
    public partial class User : IdentityUser<int>
    {
        [Column("id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public override int Id { get; set; }

        [Column("apiToken")]
        [StringLength(100)]
        public string ApiToken { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<UserArtist> ArtistRatings { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<Bookmark> Bookmarks { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<ChatMessage> ChatMessages { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<UserClaims> UserClaims { get; set; }

        [InverseProperty("Maintainer")]
        public virtual ICollection<Collection> Collections { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<Comment> Comments { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<Data.CommentReaction> CommentReactions { get; set; }

        [Column("createdDate")]
        public DateTime? CreatedDate { get; set; }

        [Column("doUseHtmlPlayer")]
        public bool? DoUseHtmlPlayer { get; set; }

        [Column("email")]
        [Required]
        [StringLength(100)]
        public override string Email { get; set; }

        [Column("ftpDirectory")]
        [StringLength(500)]
        public string FtpDirectory { get; set; }

        [Column("ftpPassword")]
        [StringLength(500)]
        public string FtpPassword { get; set; }

        [Column("ftpUrl")]
        [StringLength(250)]
        public string FtpUrl { get; set; }

        [Column("ftpUsername")]
        [StringLength(50)]
        public string FtpUsername { get; set; }

        [Column("isActive")]
        public bool? IsActive { get; set; }

        [Column("isLocked")]
        public bool? IsLocked { get; set; }

        [Column("isPrivate")]
        public bool? IsPrivate { get; set; }

        /// <summary>
        ///     This is the last time a user access Roadie via an API (ie Subsonic or Plex or Apache)
        /// </summary>
        [Column("lastApiAccess")]
        public DateTime? LastApiAccess { get; set; }

        [Column("lastFMSessionKey")]
        [StringLength(50)]
        public string LastFMSessionKey { get; set; }

        [Column("lastLogin")]
        public DateTime? LastLogin { get; set; }

        [Column("lastUpdated")]
        public DateTime? LastUpdated { get; set; }

        [Column("password")]
        [Required]
        [StringLength(100)]
        public override string PasswordHash { get; set; }

        [Column("playerTrackLimit")]
        public short? PlayerTrackLimit { get; set; }

        public virtual ICollection<Playlist> Playlists { get; set; }

        [Column("profile", TypeName = "text")]
        [StringLength(65535)]
        public string Profile { get; set; }

        [Column("randomReleaseLimit")]
        public short? RandomReleaseLimit { get; set; }

        [Column("recentlyPlayedLimit")]
        public short? RecentlyPlayedLimit { get; set; }

        [Column("registeredOn")]
        public DateTime? RegisteredOn { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<UserRelease> UserReleases { get; set; }

        [Column("removeTrackFromQueAfterPlayed")]
        public bool? RemoveTrackFromQueAfterPlayed { get; set; }

        [InverseProperty("CreatedByUser")]
        public virtual ICollection<InviteToken> InviteTokens { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<Request> Requests { get; set; }

        [Column("RoadieId")]
        [StringLength(36)]
        public Guid RoadieId { get; set; }

        [Column("status")]
        public Statuses? Status { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<Submission> Submissions { get; set; }

        [Column("timeformat")]
        [StringLength(50)]
        public string Timeformat { get; set; }

        [Column("timezone")]
        [StringLength(50)]
        public string Timezone { get; set; }

        [Column("defaultRowsPerPage")]
        public short? DefaultRowsPerPage { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<UserTrack> UserTracks { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<UserQue> UserQues { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<UsersInRoles> UserRoles { get; set; }
    }
}