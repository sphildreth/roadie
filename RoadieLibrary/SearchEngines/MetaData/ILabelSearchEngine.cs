using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.MetaData
{
    public interface ILabelSearchEngine
    {
        bool IsEnabled { get; }

        Task<OperationResult<IEnumerable<LabelSearchResult>>> PerformLabelSearch(string labelName, int resultsCount);
    }
}