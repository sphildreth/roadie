using RestSharp;
using Roadie.Library.SearchEngines.MetaData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.Imaging
{
    public interface IITunesSearchEngine : IArtistSearchEngine, IReleaseSearchEngine, IImageSearchEngine
    {
    }
}