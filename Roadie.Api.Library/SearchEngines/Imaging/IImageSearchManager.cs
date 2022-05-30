using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.Imaging
{
    public interface IImageSearchManager
    {
        Task<IEnumerable<ImageSearchResult>> ImageSearch(string query, int? resultsCount = null);
    }
}
