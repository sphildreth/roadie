using System.Collections.Generic;

namespace Roadie.Library.Data.Context
{
    public interface IRoadieDbRandomizer
    {
        SortedDictionary<int, int> RandomArtistIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        SortedDictionary<int, int> RandomGenreIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        SortedDictionary<int, int> RandomLabelIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        SortedDictionary<int, int> RandomReleaseIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        SortedDictionary<int, int> RandomTrackIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);
    }
}