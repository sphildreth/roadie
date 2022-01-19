using Roadie.Library.Caching;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Roadie.Library.SearchEngines.MetaData.LastFm
{
    public class Rootobject
    {
        public Album album { get; set; }
    }

    public class Album
    {
        public string artist { get; set; }

        public string mbid { get; set; }
       
        public string tags { get; set; }

        public string name { get; set; }

        public Image[] image { get; set; }

        public Tracks tracks { get; set; }

        public string listeners { get; set; }

        public string playcount { get; set; }

        public string url { get; set; }

        /// <summary>
        /// Sometimes LastFM returns string empty for an object (?!) and that blows up the serializer. This returns tags if the object value isn't an empty string.
        /// </summary>
        public IEnumerable<Tag> GetTags(ICacheSerializer serializer)
        {
            if(string.IsNullOrWhiteSpace(tags))
            {
                return Enumerable.Empty<Tag>();
            }
            var t = serializer.Deserialize<Tags>(tags);
            if(t != null)
            {
                return t.tag;
            }
            return Enumerable.Empty<Tag>();
        }
    }

    public class Tags
    {
        public Tag[] tag { get; set; }
    }

    public class Tag
    {
        public string url { get; set; }

        public string name { get; set; }
    }

    public class Tracks
    {
        public Track[] track { get; set; }
    }

    public class Track
    {
        public Streamable streamable { get; set; }

        public int duration { get; set; }

        public string url { get; set; }

        public string name { get; set; }

        [JsonPropertyName("@attr")]
        public Attr attr { get; set; }

        public int? TrackNumber => attr?.rank;

        public Artist artist { get; set; }
    }

    public class Streamable
    {
        public string fulltrack { get; set; }

        public string text { get; set; }
    }

    public class Attr
    {
        public int rank { get; set; }
    }

    public class Artist
    {
        public string url { get; set; }

        public string name { get; set; }

        public string mbid { get; set; }
    }

    public class Image
    {
        public string size { get; set; }

        public string text { get; set; }
    }
}
