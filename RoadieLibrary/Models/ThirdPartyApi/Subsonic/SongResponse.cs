using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{
    [Serializable]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class SongXmlResponse : BaseResponse
    {
        [XmlElement(ElementName = "song")]
        public song song { get; set; }
    }

    [DataContract]
    public class SongJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public SongJsonResponse subsonicresponse { get; set; }
    }


    [DataContract]
    public class SongJsonResponse : BaseResponse
    {
        [DataMember(Name = "song")]
        public song song { get; set; }
    }
}
