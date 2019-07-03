using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.MetaData
{
    public interface IReleaseSearchEngine
    {
        bool IsEnabled { get; }

        Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName, string query,
            int resultsCount);
    }
}