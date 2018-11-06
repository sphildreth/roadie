using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{
    [Serializable]
    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class BaseResponse
    {
        [DataMember(Name = "status")]
        [XmlAttribute(AttributeName = "status")]
        public string status { get; set; }

        [DataMember(Name = "version")]
        [XmlAttribute(AttributeName = "version")]
        public string version { get; set; }

        public BaseResponse()
        {
            this.status = "ok";
            this.version = Credentials.API_VERSION;
        }
    }
}