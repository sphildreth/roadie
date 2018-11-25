using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Data
{
    public partial class Playlist
    {
        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:playlist:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:playlist_by_id:{ Id }";
        }

        public string CacheKey
        {
            get
            {
                return Playlist.CacheUrn(this.RoadieId);
            }
        }

        public override string ToString()
        {
            return $"Id [{this.Id}], Name [{this.Name}], RoadieId [{ this.RoadieId}]";
        }
    }
}
