using System.IO;
using System.Threading.Tasks;

namespace Roadie.Library.FilePlugins
{
    public interface IFilePlugin
    {
        string[] HandlesTypes { get; }

        Task<OperationResult<bool>> Process(string destinationRoot, FileInfo file, bool doJustInfo, int? submissionId);
    }
}