using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace Roadie.Library.Identity
{
    public partial class User
    {
        public string CacheKey => CacheUrn(RoadieId);

        public string CacheKeyByUsername => CacheUrnByUsername(UserName);

        public string CacheRegion => CacheRegionUrn(RoadieId);

        /// <summary>
        ///     Returns a full file path to the User Image
        /// </summary>
        public string PathToImage(IRoadieSettings configuration, bool makeFolderIfNotExist = false)
        {
            var folder = configuration.UserImageFolder;
            if (!Directory.Exists(folder) && makeFolderIfNotExist)
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, $"{ UserName.ToFileNameFriendly() } [{ Id }].gif");
        }

        public User()
        {
            RoadieId = Guid.NewGuid();
            Status = Statuses.Ok;
            CreatedDate = DateTime.UtcNow;
            IsLocked = false;
            IsActive = true;
            DoUseHtmlPlayer = true;
            PlayerTrackLimit = 50;
            Timeformat = "YYYY-MM-DD HH:mm:ss";
            Timezone = "US/Central";
            IsPrivate = false;
            RecentlyPlayedLimit = 20;
            RandomReleaseLimit = 20;

            ArtistRatings = new HashSet<UserArtist>();
            Bookmarks = new HashSet<Bookmark>();
            Collections = new HashSet<Collection>();
            Comments = new HashSet<Comment>();
            Playlists = new HashSet<Playlist>();
            CommentReactions = new HashSet<Data.CommentReaction>();
            InviteTokens = new HashSet<InviteToken>();
            UserReleases = new HashSet<UserRelease>();
            Requests = new HashSet<Request>();
            Submissions = new HashSet<Submission>();
            UserTracks = new HashSet<UserTrack>();
            UserClaims = new HashSet<UserClaims>();
            UserQues = new HashSet<UserQue>();
            UserRoles = new HashSet<UsersInRoles>();
        }

        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:user:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:user_by_id:{Id}";
        }

        public static string CacheUrnByUsername(string Username)
        {
            return $"urn:user_by_username:{Username}";
        }

        public override string ToString()
        {
            return $"Id [{Id}], Username [{UserName}]";
        }
    }
}