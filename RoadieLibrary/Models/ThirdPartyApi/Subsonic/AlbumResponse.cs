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
    public class AlbumListXmlResponse : BaseResponse
    {
        [XmlElement(ElementName = "albumList")]
        public AlbumList albumList { get; set; }
    }

    [Serializable]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class AlbumXmlResponse : BaseResponse
    {
        [XmlElement(ElementName = "album")]
        public album album { get; set; }
    }

    [DataContract]
    public class AlbumListJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public AlbumListJsonResponse subsonicresponse { get; set; }
    }

    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class AlbumListJsonResponse : BaseResponse
    {
        [DataMember(Name = "albumList")]
        public AlbumList albumList { get; set; }
    }

    [DataContract]
    public class AlbumList
    {
        [DataMember(Name = "album")]
        [XmlElement(ElementName = "album")]
        public album[] album { get; set; }
    }



    [DataContract]
    public class AlbumJsonResponseWrapper
    {
        [DataMember(Name = "subsonic-response")]
        public AlbumJsonResponse subsonicresponse { get; set; }
    }


    [DataContract]
    [XmlRoot(ElementName = "subsonic-response", Namespace = "http://subsonic.org/restapi")]
    public class AlbumJsonResponse : BaseResponse
    {
        [DataMember(Name = "album")]
        public album album { get; set; }
    }





    //public class Song
    //{
    //    public string id { get; set; }
    //    public string parent { get; set; }
    //    public bool isDir { get; set; }
    //    public string title { get; set; }
    //    public string album { get; set; }
    //    public string artist { get; set; }
    //    public int track { get; set; }
    //    public int year { get; set; }
    //    public string genre { get; set; }
    //    public string coverArt { get; set; }
    //    public int size { get; set; }
    //    public string contentType { get; set; }
    //    public string suffix { get; set; }
    //    public int duration { get; set; }
    //    public int bitRate { get; set; }
    //    public string path { get; set; }
    //    public int userRating { get; set; }
    //    public int averageRating { get; set; }
    //    public int playCount { get; set; }
    //    public DateTime created { get; set; }
    //    public DateTime starred { get; set; }
    //    public string albumId { get; set; }
    //    public string artistId { get; set; }
    //    public string type { get; set; }
    //}


}
