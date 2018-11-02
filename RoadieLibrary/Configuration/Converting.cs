using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace roadie.Library.Setttings
{
    [Serializable]
    public class Converting
    {
        public bool DoDeleteAfter { get; set; }
        public string M4AConvertCommand { get; set; }
        public string OGGConvertCommand { get; set; }
        public string APEConvertCommand { get; set; }

        public Converting()
        {
            this.DoDeleteAfter = true;
            this.M4AConvertCommand = "ffmpeg -i \"{0}\" -acodec libmp3lame -q:a 0 \"{1}\"";
            this.OGGConvertCommand = "ffmpeg -i \"{0}\" -acodec libmp3lame -q:a 0 \"{1}\"";
            this.APEConvertCommand = "ffmpeg -i \"{0}\" \"{1}\"";
        }
    }
}
