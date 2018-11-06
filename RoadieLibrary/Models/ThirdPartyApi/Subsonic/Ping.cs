using Newtonsoft.Json;
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
    public class PingXmlResponse : BaseResponse
    {
    }

    [DataContract]
    public class PingJsonResponse
    {
        [DataMember(Name = "subsonic-response")]
        public BaseResponse subsonicresponse { get; set; }
    }
}
