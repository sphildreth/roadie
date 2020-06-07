using Roadie.Library;
using Roadie.Library.Identity;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IFileDirectoryProcessorService
    {
        IEnumerable<int> AddedArtistIds { get; }
        IEnumerable<int> AddedReleaseIds { get; }
        IEnumerable<int> AddedTrackIds { get; }

        int? ProcessLimit { get; set; }

        Task<OperationResult<bool>> ProcessAsync(User user, DirectoryInfo folder, bool doJustInfo, int? submissionId = null, bool doDeleteFiles = true);
    }
}