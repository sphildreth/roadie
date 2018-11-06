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
    public class RandomSongsXmlResponse : BaseResponse
    {
        [DataMember(Name = "randomSongs")]
        public randomSongs randomSongs { get; set; }
    }

    [DataContract]
    public class RandomSongsJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public RandomSongsJsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class RandomSongsJsonResponse : BaseResponse
    {
        [DataMember(Name = "randomSongs")]
        public randomSongs randomSongs { get; set; }
    }

    [DataContract]
    public class randomSongs
    {
        [DataMember(Name = "song")]
        public song[] song { get; set; }
    }
}
