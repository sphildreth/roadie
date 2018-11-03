using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roadie.Library.FilePlugins
{
    public interface IFilePlugin
    {
        string[] HandlesTypes { get; }
        Task<OperationResult<bool>> Process(string destinationRoot, FileInfo file, bool doJustInfo, int? submissionId);
    }
}
