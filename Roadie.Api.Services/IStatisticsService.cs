using Roadie.Library;
using Roadie.Library.Models.Statistics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IStatisticsService
    {

        Task<OperationResult<IEnumerable<DateAndCount>>> ArtistsByDateAsync();
        Task<OperationResult<LibraryStats>> LibraryStatisticsAsync();

        Task<OperationResult<IEnumerable<DateAndCount>>> ReleasesByDateAsync();

        Task<OperationResult<IEnumerable<DateAndCount>>> ReleasesByDecadeAsync();

        Task<OperationResult<IEnumerable<DateAndCount>>> SongsPlayedByDateAsync();

        Task<OperationResult<IEnumerable<DateAndCount>>> SongsPlayedByUserAsync();

    }
}