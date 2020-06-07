using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.Data.Context
{
    public interface IRoadieDbRandomizer
    {
        Task<SortedDictionary<int, int>> RandomArtistIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        Task<SortedDictionary<int, int>> RandomGenreIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        Task<SortedDictionary<int, int>> RandomLabelIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        Task<SortedDictionary<int, int>> RandomReleaseIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);

        Task<SortedDictionary<int, int>> RandomTrackIdsAsync(int userId, int randomLimit, bool doOnlyFavorites = false, bool doOnlyRated = false);
    }
}