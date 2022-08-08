using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.Imaging
{
    public interface IImageSearchEngine
    {
        bool IsEnabled { get; }

        RestRequest BuildRequest(string query, int resultsCount);

        Task<IEnumerable<ImageSearchResult>> PerformImageSearchAsync(string query, int resultsCount);
    }
}
