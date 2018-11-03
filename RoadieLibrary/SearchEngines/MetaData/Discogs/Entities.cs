using System;
using System.Collections.Generic;

namespace Roadie.Library.SearchEngines.MetaData.Discogs
{
    public class DiscogsResult
    {
        public Pagination pagination { get; set; }
        public List<Result> results { get; set; }
    }

    public class Pagination
    {
        public int? per_page { get; set; }
        public int? items { get; set; }
        public int? page { get; set; }
        public Urls urls { get; set; }
        public int? pages { get; set; }
    }

    public class Urls
    {
        public string last { get; set; }
        public string next { get; set; }
    }

    public class Result
    {
        public string thumb { get; set; }
        public string title { get; set; }
        public string uri { get; set; }
        public string resource_url { get; set; }
        public string type { get; set; }
        public int? id { get; set; }
    }

    public class DiscogArtistResponse
    {
        public string profile { get; set; }
        public string releases_url { get; set; }
        public string name { get; set; }
        public List<string> namevariations { get; set; }
        public string uri { get; set; }
        public List<string> urls { get; set; }
        public List<Image> images { get; set; }
        public string resource_url { get; set; }
        public List<Group> groups { get; set; }
        public int? id { get; set; }
        public string data_quality { get; set; }
        public string realname { get; set; }
    }

    public class Image
    {
        public string uri { get; set; }
        public int? height { get; set; }
        public int? width { get; set; }
        public string resource_url { get; set; }
        public string type { get; set; }
        public string uri150 { get; set; }
    }

    public class Group
    {
        public bool active { get; set; }
        public string resource_url { get; set; }
        public int? id { get; set; }
        public string name { get; set; }
    }

    public class DiscogsReleaseSearchResult
    {
        public Pagination pagination { get; set; }
        public List<ReleaseSearchRelease> results { get; set; }
    }

    public class ReleaseSearchRelease
    {
        public List<string> style { get; set; }
        public string thumb { get; set; }
        public List<string> format { get; set; }
        public string country { get; set; }
        public List<string> barcode { get; set; }
        public string uri { get; set; }
        public Community community { get; set; }
        public List<string> label { get; set; }
        public string catno { get; set; }
        public string year { get; set; }
        public List<string> genre { get; set; }
        public string title { get; set; }
        public string resource_url { get; set; }
        public string type { get; set; }
        public int? id { get; set; }
    }

    public class Community
    {
        public string status { get; set; }
        public Rating rating { get; set; }
        public int? want { get; set; }
        public List<Contributor> contributors { get; set; }
        public int? have { get; set; }
        public Submitter submitter { get; set; }
        public string data_quality { get; set; }
    }

    public class DiscogReleaseDetail
    {
        public List<string> styles { get; set; }
        public List<Video> videos { get; set; }
        public List<string> series { get; set; }
        public List<Label> labels { get; set; }
        public Community community { get; set; }
        public int? year { get; set; }
        public List<Image> images { get; set; }
        public int? format_quantity { get; set; }
        public int? id { get; set; }
        public List<string> genres { get; set; }
        public string thumb { get; set; }
        public List<Extraartist> extraartists { get; set; }
        public string title { get; set; }
        public List<Artist> artists { get; set; }
        public DateTime date_changed { get; set; }
        public int? master_id { get; set; }
        public List<Tracklist> tracklist { get; set; }
        public string status { get; set; }
        public string released_formatted { get; set; }
        public int? estimated_weight { get; set; }
        public string master_url { get; set; }
        public string released { get; set; }
        public DateTime date_added { get; set; }
        public string country { get; set; }
        public string notes { get; set; }
        public List<Identifier> identifiers { get; set; }
        public List<Company> companies { get; set; }
        public string uri { get; set; }
        public List<Format> formats { get; set; }
        public string resource_url { get; set; }
        public string data_quality { get; set; }
    }

    public class Rating
    {
        public int? count { get; set; }
        public float average { get; set; }
    }

    public class Submitter
    {
        public string username { get; set; }
        public string resource_url { get; set; }
    }

    public class Contributor
    {
        public string username { get; set; }
        public string resource_url { get; set; }
    }

    public class Video
    {
        public int? duration { get; set; }
        public bool embed { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string uri { get; set; }
    }

    public class Label
    {
        public string name { get; set; }
        public string entity_type { get; set; }
        public string catno { get; set; }
        public string resource_url { get; set; }
        public int? id { get; set; }
        public string entity_type_name { get; set; }
    }

    public class Extraartist
    {
        public string join { get; set; }
        public string name { get; set; }
        public string anv { get; set; }
        public string tracks { get; set; }
        public string role { get; set; }
        public string resource_url { get; set; }
        public int? id { get; set; }
    }

    public class Artist
    {
        public string join { get; set; }
        public string name { get; set; }
        public string anv { get; set; }
        public string tracks { get; set; }
        public string role { get; set; }
        public string resource_url { get; set; }
        public int? id { get; set; }
    }

    public class Tracklist
    {
        public string duration { get; set; }
        public string position { get; set; }
        public string type_ { get; set; }
        public string title { get; set; }
    }

    public class Identifier
    {
        public string type { get; set; }
        public string value { get; set; }
        public string description { get; set; }
    }

    public class Company
    {
        public string name { get; set; }
        public string entity_type { get; set; }
        public string catno { get; set; }
        public string resource_url { get; set; }
        public int? id { get; set; }
        public string entity_type_name { get; set; }
    }

    public class Format
    {
        public string qty { get; set; }
        public List<string> descriptions { get; set; }
        public string name { get; set; }
    }

    public class DiscogsLabelResult
    {
        public string profile { get; set; }
        public string releases_url { get; set; }
        public string name { get; set; }
        public string contact_info { get; set; }
        public string uri { get; set; }
        public List<Sublabel> sublabels { get; set; }
        public List<string> urls { get; set; }
        public List<Image> images { get; set; }
        public string resource_url { get; set; }
        public int? id { get; set; }
        public string data_quality { get; set; }
    }

    public class Sublabel
    {
        public string resource_url { get; set; }
        public int? id { get; set; }
        public string name { get; set; }
    }
}