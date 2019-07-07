using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Roadie.Library.Encoding;
using Roadie.Library.FilePlugins;

namespace Roadie.Library.Processors
{
    public interface IFileProcessor
    {
        IHttpEncoder HttpEncoder { get; }
        IEnumerable<IFilePlugin> Plugins { get; }
        int? SubmissionId { get; set; }

        Task<OperationResult<bool>> Process(FileInfo fileInfo, bool doJustInfo = false);
        Task<OperationResult<bool>> Process(string filename, bool doJustInfo = false);
    }
}