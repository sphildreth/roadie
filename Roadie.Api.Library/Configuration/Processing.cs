using System;
using System.Collections.Generic;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public class Processing : IProcessing
    {
        public bool DoAudioCleanup { get; set; }
        public bool DoClearComments { get; set; }
        public bool DoDeleteUnknowns { get; set; }
        public bool DoFolderArtistNameSet { get; set; }
        public bool DoMoveUnknowns { get; set; }
        public bool DoParseFromDiscogsDB { get; private set; }
        public bool DoParseFromDiscogsDBFindingTrackForArtist { get; private set; }
        public bool DoParseFromFileName { get; set; }
        public bool DoParseFromLastFM { get; private set; }
        public bool DoParseFromMusicBrainz { get; private set; }
        public bool DoSaveEditsToTags { get; set; }
        public int MaxImageWidth { get; set; }
        public int MaximumArtistImagesToAdd { get; set; }
        public int MaximumReleaseImagesToAdd { get; set; }
        public string RemoveStringsRegex { get; set; }
        public string ArtistRemoveStringsRegex { get; set; }
        public string ReleaseRemoveStringsRegex { get; set; }
        public string TrackRemoveStringsRegex { get; set; }

        public List<ReplacementString> ReplaceStrings { get; set; }
        public string UnknownFolder { get; set; }

        public Processing()
        {
            this.ReplaceStrings = new List<ReplacementString>();
            this.DoAudioCleanup = true;
            this.DoClearComments = true;
        }
    }
}