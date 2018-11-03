using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.Imaging
{
    public interface IImageSearchEngine
    {
        Task<IEnumerable<ImageSearchResult>> PerformImageSearch(string query, int resultsCount);

        RestRequest BuildRequest(string query, int resultsCount);
    }
}