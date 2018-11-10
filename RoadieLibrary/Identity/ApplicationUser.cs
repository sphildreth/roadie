using Microsoft.AspNetCore.Identity;
using Roadie.Library.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Identity
{
    [Table("user")]
    public partial class ApplicationUser : IdentityUser<int>
    {
        [Column("apiToken")]
        [StringLength(100)]
        public string ApiToken { get; set; }

        public virtual ICollection<UserArtist> ArtistRatings { get; set; }

        [Column("avatar", TypeName = "blob")]
        public byte[] Avatar { get; set; }

        public ICollection<Bookmark> Bookmarks { get; set; }

        public virtual ICollection<ApplicationUserClaim> Claims { get; set; }

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

        [Column("id")]
        [Key]
        public override int Id { get; set; }

        [Column("isActive")]
        public bool? IsActive { get; set; }

        [Column("isLocked")]
        public bool? IsLocked { get; set; }

        [Column("isPrivate")]
        public bool? IsPrivate { get; set; }

        [Column("lastApiAccess")]
        public DateTime? LastApiAccess { get; set; }

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

        public ICollection<Playlist> Playlists { get; set; }

        [Column("profile", TypeName = "text")]
        [StringLength(65535)]
        public string Profile { get; set; }

        [Column("randomReleaseLimit")]
        public short? RandomReleaseLimit { get; set; }

        [Column("recentlyPlayedLimit")]
        public short? RecentlyPlayedLimit { get; set; }

        [Column("registeredOn")]
        public DateTime? RegisteredOn { get; set; }

        public ICollection<UserRelease> ReleaseRatings { get; set; }

        public ICollection<Request> Requests { get; set; }

        [Column("RoadieId")]
        [StringLength(36)]
        public Guid RoadieId { get; set; }

        //    public virtual ICollection<UsersInRoles> Roles { get; set; }

        [Column("status")]
        public short? Status { get; set; }

        public ICollection<Submission> Submissions { get; set; }

        [Column("timeformat")]
        [StringLength(50)]
        public string Timeformat { get; set; }

        [Column("timezone")]
        [StringLength(50)]
        public string Timezone { get; set; }

        public ICollection<UserTrack> TrackRatings { get; set; }

        [Column("username")]
        [Required]
        [StringLength(20)]
        public string Username { get; set; }

        public virtual ICollection<ApplicationUserRole> UserRoles { get; set; }
    }
}