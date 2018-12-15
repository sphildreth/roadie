using Roadie.Library.Data;
using Roadie.Library.MetaData.Audio;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.Factories
{
    public interface IReleaseFactory
    {
        IEnumerable<int> AddedReleaseIds { get; }
        IEnumerable<int> AddedTrackIds { get; }


        Task<OperationResult<bool>> CheckAndChangeReleaseTitle(Release release, string oldReleaseFolder, string destinationFolder = null);

        Task<OperationResult<bool>> Delete(Release release, bool doDeleteFiles = false);

        Task<OperationResult<bool>> DeleteReleases(IEnumerable<Guid> releaseIds, bool doDeleteFiles = false);

        OperationResult<Release> GetAllForArtist(Artist artist, bool forceRefresh = false);

        Task<OperationResult<bool>> MergeReleases(Release releaseToMerge, Release releaseToMergeInto, bool addAsMedia);        

        Task<OperationResult<bool>> ScanReleaseFolder(Guid releaseId, string destinationFolder, bool doJustInfo, Release releaseToScan = null);

        Task<OperationResult<Release>> Update(Release release, IEnumerable<Image> releaseImages, string originalReleaseFolder, string destinationFolder = null);
    }
}