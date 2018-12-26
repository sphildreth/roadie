using Roadie.Library.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.Factories
{
    public interface IArtistFactory
    {
        Task<OperationResult<bool>> Delete(Artist Artist);

        Task<OperationResult<bool>> Delete(Guid RoadieId);

        OperationResult<Artist> GetByExternalIds(string musicBrainzId = null, string iTunesId = null, string amgId = null, string spotifyId = null);

        Task<OperationResult<Artist>> MergeArtists(Artist ArtistToMerge, Artist artistToMergeInto, bool doDbUpdates = false);

        Task<OperationResult<bool>> RefreshArtistMetadata(Guid ArtistId);

        Task<OperationResult<bool>> ScanArtistReleasesFolders(Guid artistId, string destinationFolder, bool doJustInfo);

        Task<OperationResult<Artist>> Update(Artist Artist, IEnumerable<Image> ArtistImages, string destinationFolder = null);
    }
}