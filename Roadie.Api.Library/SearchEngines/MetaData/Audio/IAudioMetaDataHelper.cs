using System.IO;
using System.Threading.Tasks;

namespace Roadie.Library.MetaData.Audio
{
    public interface IAudioMetaDataHelper
    {
        bool DoParseFromDiscogsDB { get; set; }

        bool DoParseFromDiscogsDBFindingTrackForArtist { get; set; }

        bool DoParseFromFileName { get; set; }

        bool DoParseFromLastFM { get; set; }

        bool DoParseFromMusicBrainz { get; set; }

        Task<AudioMetaData> GetInfo(FileInfo fileInfo, bool doJustInfo = false);

        bool WriteTags(AudioMetaData metaData, FileInfo fileInfo);
    }
}
