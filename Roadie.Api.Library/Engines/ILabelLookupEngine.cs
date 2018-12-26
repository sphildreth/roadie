using System.Threading.Tasks;
using Roadie.Library.Data;

namespace Roadie.Library.Engines
{
    public interface ILabelLookupEngine
    {
        Task<OperationResult<Label>> Add(Label label);
        Task<OperationResult<Label>> GetByName(string LabelName, bool doFindIfNotInDatabase = false);
        Task<OperationResult<Label>> PerformMetaDataProvidersLabelSearch(string LabelName);
    }
}