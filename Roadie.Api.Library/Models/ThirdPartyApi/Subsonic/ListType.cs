using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.ThirdPartyApi.Subsonic
{
    public enum ListType : short
    {
        Unknown = 0,
        Random,
        Newest,
        Highest,
        Frequent,
        Recent,
        AlphabeticalByName,
        AlphabeticalByArtist,
        Starred,
        ByYear,
        ByGenre
    }
}
