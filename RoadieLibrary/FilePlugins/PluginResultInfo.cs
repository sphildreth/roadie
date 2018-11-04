using Roadie.Library.Enums;
using System;
using System.Collections.Generic;

namespace Roadie.Library.FilePlugins
{
    [Serializable]
    public class PluginResultInfo : OperationResultModel
    {
        public const string AdditionalDataKeyPluginResultInfo = "PluginResultInfo";

        public string ArtistFolder { get; set; }
        public Guid ArtistId { get; set; }
        public IEnumerable<string> ArtistNames { get; set; }
        public string Filename { get; set; }
        public string ReleaseFolder { get; set; }
        public Guid ReleaseId { get; set; }
        public Statuses Status { get; set; }
        public short? TrackNumber { get; set; }
        public string TrackTitle { get; set; }

        public PluginResultInfo()
        {
            this.Status = Statuses.Incomplete;
        }
    }
}