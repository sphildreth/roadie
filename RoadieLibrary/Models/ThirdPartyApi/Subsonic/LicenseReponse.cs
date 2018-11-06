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
    public class LicenseXmlResponse : BaseResponse
    {
        [XmlElement(ElementName = "license")]
        public License license { get; set; }
    }

    [DataContract]
    public class LicenseJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public LicenseJsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class LicenseJsonResponse : BaseResponse
    {
        [DataMember(Name = "license")]
        public License license { get; set; }
    }
}
