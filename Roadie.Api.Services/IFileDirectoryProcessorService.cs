using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Roadie.Library;
using Roadie.Library.Identity;

namespace Roadie.Api.Services
{
    public interface IFileDirectoryProcessorService
    {
        IEnumerable<int> AddedArtistIds { get; }
        IEnumerable<int> AddedReleaseIds { get; }
        IEnumerable<int> AddedTrackIds { get; }

        int? ProcessLimit { get; set; }

        Task<OperationResult<bool>> Process(ApplicationUser user, DirectoryInfo folder, bool doJustInfo, int? submissionId = null);
    }
}