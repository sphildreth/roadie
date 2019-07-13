using System.Collections.Generic;

namespace Roadie.Library.Configuration
{
    public interface IProcessing
    {
        string ArtistRemoveStringsRegex { get; set; }
        bool DoAudioCleanup { get; set; }
        bool DoClearComments { get; set; }
        bool DoDeleteUnknowns { get; set; }
        bool DoMoveUnknowns { get; set; }
        bool DoParseFromDiscogsDB { get; }
        bool DoParseFromFileName { get; set; }
        bool DoParseFromLastFM { get; }
        bool DoParseFromMusicBrainz { get; }
        bool DoSaveEditsToTags { get; set; }
        int MaxImageWidth { get; set; }
        int MaximumArtistImagesToAdd { get; set; }
        int MaximumReleaseImagesToAdd { get; set; }
        string ReleaseRemoveStringsRegex { get; set; }
        string RemoveStringsRegex { get; set; }
        List<ReplacementString> ReplaceStrings { get; set; }
        string TrackRemoveStringsRegex { get; set; }
        string UnknownFolder { get; set; }
    }
}