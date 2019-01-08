using Roadie.Library;
using Roadie.Library.Models.Statistics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IStatisticsService
    {
        Task<OperationResult<LibraryStats>> LibraryStatistics();

        Task<OperationResult<IEnumerable<DateAndCount>>> ReleasesByDate();
    }
}