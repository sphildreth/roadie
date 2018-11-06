using System;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public class ReleaseGenre : EntityModelBase
    {
        public Genre Genre { get; set; }
    }
}