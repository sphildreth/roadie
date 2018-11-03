using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.MetaData
{
    public interface IArtistSearchEngine
    {
        bool IsEnabled { get; }

        Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearch(string query, int resultsCount);
    }
}