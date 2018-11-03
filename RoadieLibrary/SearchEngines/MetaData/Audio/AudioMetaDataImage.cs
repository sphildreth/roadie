using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roadie.Library.MetaData.Audio
{
    public sealed class AudioMetaDataImage
    {
        public string Url { get; set; }
        public byte[] Data { get; set; }
        public string Description { get; set; }
        public string MimeType { get; set; }
        public AudioMetaDataImageType Type { get; set; }
    }
}
