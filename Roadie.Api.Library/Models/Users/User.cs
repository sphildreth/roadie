using Mapster;
using Roadie.Library.Models.Statistics;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Models.Users
{
    [Serializable]
    public class User : EntityModelBase
    {
        public const string ActionKeyUserRated = "__userrated__";
        public const string DefaultIncludes = "stats";
        [MaxLength(100)] public string ApiToken { get; set; }

        public Image Avatar { get; set; }

        /// <summary>
        ///     Posted image from a client of selected new base64 encoded avatar for the user
        /// </summary>
        public string AvatarData { get; set; }

        [Required] [MaxLength(100)] public string ConcurrencyStamp { get; set; }
        public bool DoUseHtmlPlayer { get; set; }

        [Required]
        [MaxLength(100)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [MaxLength(500)] public string FtpDirectory { get; set; }

        [MaxLength(500)] public string FtpPassword { get; set; }

        [MaxLength(250)] public string FtpUrl { get; set; }

        [MaxLength(50)] public string FtpUsername { get; set; }
        public new int? Id { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsEditor { get; set; }
        public bool IsPrivate { get; set; }

        /// <summary>
        ///     Posted password only used when changing password from profile edits
        /// </summary>
        [NotMapped]
        [AdaptIgnore]
        public string Password { get; set; }

        /// <summary>
        ///     Posted password confirmation only used when changing password from profile edits
        /// </summary>
        [NotMapped]
        [AdaptIgnore]
        public string PasswordConfirmation { get; set; }

        public short? PlayerTrackLimit { get; set; }
        [MaxLength(65535)] public string Profile { get; set; }
        public short? RandomReleaseLimit { get; set; }
        public short? RecentlyPlayedLimit { get; set; }
        public bool RemoveTrackFromQueAfterPlayed { get; set; }
        [NotMapped] [AdaptIgnore] public UserStatistics Statistics { get; set; }
        [StringLength(50)] [Required] public string Timeformat { get; set; }

        [MaxLength(50)] [Required] public string Timezone { get; set; }

        [AdaptMember("RoadieId")] public Guid UserId { get; set; }

        [Required] [MaxLength(20)] public string UserName { get; set; }
        public Image MediumThumbnail { get; set; }

        public DateTime LastLogin { get; set; }
        public DateTime LastApiAccess { get; set; }
        public short? DefaultRowsPerPage { get; set; }

        public override string ToString() => $"Id [{Id}], RoadieId [{UserId}], UserName [{UserName}]";
    }
}