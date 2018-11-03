using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roadie.Library.Setttings
{
    [Serializable]
    public class Processing
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
        public Dictionary<string,string> ReplaceStrings { get; set; }
        public int MaximumArtistImagesToAdd { get; set; }
        public int MaximumReleaseImagesToAdd { get; set; }
        public string RemoveStringsRegex { get; set; }
        public string UnknownFolder { get; set; }

        public Processing()
        {
            this.DoAudioCleanup = true;
            this.DoSaveEditsToTags = true;
            this.DoClearComments = true;
            this.ReplaceStrings = new Dictionary<string, string>();
            this.ReplaceStrings.Add("-OBSERVER", "");
            this.ReplaceStrings.Add("[Torrent Tatty]", "");
            this.ReplaceStrings.Add("^", "");
            this.ReplaceStrings.Add("_", " ");
            this.ReplaceStrings.Add("-", " ");
            this.ReplaceStrings.Add("~", ",");

            this.RemoveStringsRegex = @"\b[0-9]+\s#\s\b";

            this.MaximumArtistImagesToAdd = 12;
            this.MaximumReleaseImagesToAdd = 12;

            this.DoParseFromFileName = true;
            this.DoParseFromDiscogsDBFindingTrackForArtist = true;
            this.DoParseFromDiscogsDB = true;
            this.DoParseFromMusicBrainz = true;
            this.DoParseFromLastFM = true;

        }
    }
}
