using System.Threading.Tasks;
using Roadie.Library;
using Roadie.Library.Models.Statistics;

namespace Roadie.Api.Services
{
    public interface IStatisticsService
    {
        Task<OperationResult<LibraryStats>> LibraryStatistics();
    }
}