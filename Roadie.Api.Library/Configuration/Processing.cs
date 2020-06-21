using System;
using System.Collections.Generic;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public class Processing : IProcessing
    {
        public string ArtistRemoveStringsRegex { get; set; }

        public bool DoAudioCleanup { get; set; }

        public bool DoClearComments { get; set; }

        public bool DoDeleteUnknowns { get; set; }

        public bool DoMoveUnknowns { get; set; }

        public bool DoParseFromDiscogsDB { get; private set; }

        public bool DoParseFromFileName { get; set; }

        public bool DoParseFromLastFM { get; private set; }

        public bool DoParseFromMusicBrainz { get; private set; }

        public bool DoSaveEditsToTags { get; set; }

        public int MaxImageWidth { get; set; }

        public int MaximumArtistImagesToAdd { get; set; }

        public int MaximumReleaseImagesToAdd { get; set; }

        public string PostInspectScript { get; set; }

        public string PreInspectScript { get; set; }

        public string ReleaseRemoveStringsRegex { get; set; }

        public string RemoveStringsRegex { get; set; }

        public List<ReplacementString> ReplaceStrings { get; set; }

        public string TrackRemoveStringsRegex { get; set; }

        public string UnknownFolder { get; set; }

        public bool DoDetectFeatureFragments { get; set; }

        public Processing()
        {
            DoAudioCleanup = true;
            DoClearComments = true;
            DoParseFromDiscogsDB = true;
            DoParseFromFileName = true;
            DoParseFromLastFM = true;
            DoParseFromMusicBrainz = true;
            DoSaveEditsToTags = true;
            DoDetectFeatureFragments = true;

            MaximumArtistImagesToAdd = 12;
            MaximumReleaseImagesToAdd = 12;

            MaxImageWidth = 2048;

            RemoveStringsRegex = "\\b[0-9]+\\s#\\s\\b";
            ReplaceStrings = new List<ReplacementString>();
        }
    }
}