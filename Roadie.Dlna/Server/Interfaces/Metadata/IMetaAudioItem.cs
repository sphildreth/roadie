using System;

namespace Roadie.Dlna.Server.Metadata
{
    public interface IMetaAudioItem : IMetaInfo, IMetaDescription, IMetaDuration, IMetaGenre
    {
        int? MetaReleaseYear { get; }

        string MetaAlbum { get; }

        string MetaArtist { get; }

        string MetaPerformer { get; }

        int? MetaTrack { get; }
    }
}