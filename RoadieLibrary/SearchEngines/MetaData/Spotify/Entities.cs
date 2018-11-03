using System.Collections.Generic;

namespace Roadie.Library.SearchEngines.MetaData.Spotify
{
    public class SpotifyResult
    {
        public Artists artists { get; set; }
    }

    public class Artists
    {
        public string href { get; set; }
        public List<Item> items { get; set; }
        public int? limit { get; set; }
        public string next { get; set; }
        public int? offset { get; set; }
        public string previous { get; set; }
        public int? total { get; set; }
    }

    public class Item
    {
        public External_Urls external_urls { get; set; }
        public Followers followers { get; set; }
        public List<string> genres { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public List<Image> images { get; set; }
        public string name { get; set; }
        public int? popularity { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
        public string album_type { get; set; }
        public List<string> available_markets { get; set; }
    }

    public class External_Urls
    {
        public string spotify { get; set; }
    }

    public class Followers
    {
        public string href { get; set; }
        public int? total { get; set; }
    }

    public class Image
    {
        public int? height { get; set; }
        public string url { get; set; }
        public int? width { get; set; }
    }

    public class AlbumSearchResult
    {
        public Albums albums { get; set; }
    }

    public class Albums
    {
        public string href { get; set; }
        public List<Item> items { get; set; }
        public int? limit { get; set; }
        public string next { get; set; }
        public int? offset { get; set; }
        public string previous { get; set; }
        public int? total { get; set; }
    }

    public class AlbumResult
    {
        public string album_type { get; set; }
        public List<Artist1> artists { get; set; }
        public List<string> available_markets { get; set; }
        public List<Copyright> copyrights { get; set; }
        public External_Ids external_ids { get; set; }
        public External_Urls external_urls { get; set; }
        public List<string> genres { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public List<Image> images { get; set; }
        public string name { get; set; }
        public int? popularity { get; set; }
        public string release_date { get; set; }
        public string release_date_precision { get; set; }
        public Tracks tracks { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }

    public class External_Ids
    {
        public string upc { get; set; }
    }

    public class Tracks
    {
        public string href { get; set; }
        public List<Track> items { get; set; }
        public int? limit { get; set; }
        public string next { get; set; }
        public int? offset { get; set; }
        public string previous { get; set; }
        public int? total { get; set; }
    }

    public class Track
    {
        public List<Artist> artists { get; set; }
        public List<string> available_markets { get; set; }
        public int? disc_number { get; set; }
        public int? duration_ms { get; set; }
        public bool _explicit { get; set; }
        public External_Urls1 external_urls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string preview_url { get; set; }
        public short? track_number { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }

    public class External_Urls1
    {
        public string spotify { get; set; }
    }

    public class Artist
    {
        public External_Urls2 external_urls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }

    public class External_Urls2
    {
        public string spotify { get; set; }
    }

    public class Artist1
    {
        public External_Urls3 external_urls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }

    public class External_Urls3
    {
        public string spotify { get; set; }
    }

    public class Copyright
    {
        public string text { get; set; }
        public string type { get; set; }
    }
}