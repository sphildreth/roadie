using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;
using Roadie.Library.SearchEngines.MetaData;

namespace Roadie.Library.SearchEngines.Imaging
{
    public interface IITunesSearchEngine
    {
        bool IsEnabled { get; }

        RestRequest BuildRequest(string query, int resultsCount);
        Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearch(string query, int resultsCount);
        Task<IEnumerable<ImageSearchResult>> PerformImageSearch(string query, int resultsCount);
        Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName, string query, int resultsCount);
    }
}