using Roadie.Library;
using Roadie.Library.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ILookupService
    {
        Task<OperationResult<IEnumerable<DataToken>>> ArtistTypesAsync();

        Task<OperationResult<IEnumerable<DataToken>>> BandStatusAsync();

        Task<OperationResult<IEnumerable<DataToken>>> BookmarkTypesAsync();

        Task<OperationResult<IEnumerable<DataToken>>> CollectionTypesAsync();

        Task<OperationResult<IEnumerable<DataToken>>> CreditCategoriesAsync();

        Task<OperationResult<IEnumerable<DataToken>>> LibraryStatusAsync();

        Task<OperationResult<IEnumerable<DataToken>>> QueMessageTypesAsync();

        Task<OperationResult<IEnumerable<DataToken>>> ReleaseTypesAsync();

        Task<OperationResult<IEnumerable<DataToken>>> RequestStatusAsync();

        Task<OperationResult<IEnumerable<DataToken>>> StatusAsync();
    }
}