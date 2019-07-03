using Roadie.Library.Data;
using Roadie.Library.Enums;
using System;
using System.Collections.Generic;

namespace Roadie.Library.Identity
{
    public partial class ApplicationUser
    {
        public string CacheKey => CacheUrn(RoadieId);

        public string CacheKeyByUsername => CacheUrnByUsername(UserName);

        public string CacheRegion => CacheRegionUrn(RoadieId);

        public ApplicationUser()
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

            //  Collections = new HashSet<Collection>();
            Playlists = new HashSet<Playlist>();
            UserRoles = new HashSet<ApplicationUserRole>();
            Requests = new HashSet<Request>();
            Submissions = new HashSet<Submission>();
            UserQues = new HashSet<UserQue>();
            ArtistRatings = new HashSet<UserArtist>();
            ReleaseRatings = new HashSet<UserRelease>();
            TrackRatings = new HashSet<UserTrack>();
            Comments = new HashSet<Comment>();
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