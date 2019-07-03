using System;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public class ReleaseSubmission : EntityInfoModelBase
    {
        public DateTime? SubmittedDate { get; set; }
        public DataToken User { get; set; }
        public Image UserThumbnail { get; set; }
    }
}