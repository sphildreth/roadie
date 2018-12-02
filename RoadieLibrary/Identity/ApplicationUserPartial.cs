using Roadie.Library.Data;
using Roadie.Library.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Identity
{
    public partial class ApplicationUser
    {
        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:user:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:user_by_id:{ Id }";
        }

        public static string CacheUrnByUsername(string Username)
        {
            return $"urn:user_by_username:{ Username }";
        }

        public string CacheRegion
        {
            get
            {
                return ApplicationUser.CacheRegionUrn(this.RoadieId);
            }
        }

        public string CacheKeyByUsername
        {
            get
            {
                return ApplicationUser.CacheUrnByUsername(this.UserName);
            }
        }

        public string CacheKey
        {
            get
            {
                return ApplicationUser.CacheUrn(this.RoadieId);
            }
        }

        public override string ToString()
        {
            return $"Id [{ this.Id }], Username [{ this.UserName}]";
        }

        public ApplicationUser()
        {
            this.RoadieId = Guid.NewGuid();
            this.Status = Statuses.Ok;
            this.CreatedDate = DateTime.UtcNow;
            this.IsLocked = false;
            this.IsActive = true;
            this.DoUseHtmlPlayer = true;
            this.PlayerTrackLimit = 50;
            this.Timeformat = "YYYY-MM-DD HH:mm:ss";
            this.IsPrivate = false;
            this.RecentlyPlayedLimit = 20;
            this.RandomReleaseLimit = 20;

          //  Collections = new HashSet<Collection>();
            Playlists = new HashSet<Playlist>();
            Requests = new HashSet<Request>();
            Submissions = new HashSet<Submission>();
            UserQues = new HashSet<UserQue>();
            ArtistRatings = new HashSet<UserArtist>();
            ReleaseRatings = new HashSet<UserRelease>();
            TrackRatings = new HashSet<UserTrack>();

        }
    }
}
