using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{
    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class ArtistXmlResponse : BaseResponse
    {
        [XmlElement(ElementName = "artist")]
        public artist artist { get; set; }
    }

    [DataContract]
    public class ArtistJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public ArtistResponse subsonicresponse { get; set; }
    }

    [DataContract]
    public class ArtistResponse : BaseResponse
    {
        [DataMember(Name = "artist")]
        public artist artist { get; set; }
    }
}
