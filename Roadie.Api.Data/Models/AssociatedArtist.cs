using Mapster;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Api.Data.Models
{
    [Serializable]
    public class AssociatedArtist
    {
        [AdaptMember("AssociatedArtist")]
        public AssociatedArtistInfo AssociatedArtistInfo { get; set; }
    }
}
