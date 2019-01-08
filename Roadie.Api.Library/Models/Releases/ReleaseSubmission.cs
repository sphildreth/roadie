using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public class ReleaseSubmission : EntityInfoModelBase
    {
        public DataToken User { get; set; }
        public Image UserThumbnail { get; set; }
        public DateTime? SubmittedDate { get; set; }
    }
}
