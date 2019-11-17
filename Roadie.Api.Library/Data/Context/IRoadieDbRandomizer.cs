using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.Data.Context
{
    public interface IRoadieDbRandomizer
    {
        Task<SortedDictionary<int, int>> RandomArtistIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        Task<SortedDictionary<int, int>> RandomGenreIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        Task<SortedDictionary<int, int>> RandomLabelIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        Task<SortedDictionary<int, int>> RandomReleaseIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        Task<SortedDictionary<int, int>> RandomTrackIds(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);
    }
}