using Roadie.Library;
using Roadie.Library.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ILookupService
    {
        Task<OperationResult<IEnumerable<DataToken>>> ArtistTypes();

        Task<OperationResult<IEnumerable<DataToken>>> BandStatus();

        Task<OperationResult<IEnumerable<DataToken>>> BookmarkTypes();

        Task<OperationResult<IEnumerable<DataToken>>> CollectionTypes();

        Task<OperationResult<IEnumerable<DataToken>>> LibraryStatus();

        Task<OperationResult<IEnumerable<DataToken>>> QueMessageTypes();

        Task<OperationResult<IEnumerable<DataToken>>> ReleaseTypes();

        Task<OperationResult<IEnumerable<DataToken>>> RequestStatus();

        Task<OperationResult<IEnumerable<DataToken>>> Status();

        Task<OperationResult<IEnumerable<DataToken>>> CreditCategories();
    }
}