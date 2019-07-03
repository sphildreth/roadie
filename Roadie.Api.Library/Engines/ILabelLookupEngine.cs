using Roadie.Library.Data;
using System.Threading.Tasks;

namespace Roadie.Library.Engines
{
    public interface ILabelLookupEngine
    {
        Task<OperationResult<Label>> Add(Label label);

        Task<OperationResult<Label>> GetByName(string LabelName, bool doFindIfNotInDatabase = false);

        Task<OperationResult<Label>> PerformMetaDataProvidersLabelSearch(string LabelName);
    }
}