using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.Imaging
{
    public interface IImageSearchEngine
    {
        bool IsEnabled { get; }

        RestRequest BuildRequest(string query, int resultsCount);

        Task<IEnumerable<ImageSearchResult>> PerformImageSearch(string query, int resultsCount);
    }
}