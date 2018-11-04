using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Roadie.Library.MetaData.MusicBrainz
{
    public class Alias
    {
        public object begindate { get; set; }

        public object enddate { get; set; }

        public object locale { get; set; }

        public string name { get; set; }

        public object primary { get; set; }

        [JsonProperty(PropertyName = "sort-name")]
        public string sortname { get; set; }

        public object type { get; set; }
    }

    public class Area
    {
        public string disambiguation { get; set; }
        public string id { get; set; }
        public List<string> iso31661codes { get; set; }
        public string name { get; set; }
        public string sortname { get; set; }
    }

    [DebuggerDisplay("name: {name}")]
    public class Artist
    {
        public List<Alias> aliases { get; set; }
        public Area area { get; set; }
        public Begin_Area begin_area { get; set; }
        public string country { get; set; }

        public string disambiguation { get; set; }
        public object end_area { get; set; }
        public string gender { get; set; }
        public string id { get; set; }
        public List<string> ipis { get; set; }

        [JsonProperty(PropertyName = "isni-list")]
        public List<string> isnis { get; set; }

        public LifeSpan lifespan { get; set; }

        public string name { get; set; }

        public List<Release> releases { get; set; }

        [JsonProperty(PropertyName = "sort-name")]
        public string sortname { get; set; }

        public List<Tag> tags { get; set; }
        public string type { get; set; }
    }

    public class ArtistResult
    {
        public List<Artist> artists { get; set; }
        public int? count { get; set; }
        public DateTime? created { get; set; }
        public int? offset { get; set; }
    }

    public class AttributeValues
    {
    }

    public class Begin_Area
    {
        public string disambiguation { get; set; }

        public string id { get; set; }
        public List<string> iso_3166_1_codes { get; set; }
        public List<string> iso_3166_2_codes { get; set; }
        public List<string> iso_3166_3_codes { get; set; }

        public string name { get; set; }
        public string sortname { get; set; }
    }

    public class BeginArea
    {
        public string id { get; set; }

        public string name { get; set; }

        public string sortname { get; set; }
    }

    public class CoverArtArchive
    {
        public bool artwork { get; set; }
        public bool back { get; set; }
        public int? count { get; set; }

        public bool darkened { get; set; }
        public bool front { get; set; }
    }

    public class Label
    {
        public List<Alias> aliases { get; set; }
        public string disambiguation { get; set; }
        public string id { get; set; }

        [JsonProperty(PropertyName = "label-code")]
        public int? labelcode { get; set; }

        public string name { get; set; }

        [JsonProperty(PropertyName = "sort-name")]
        public string sortname { get; set; }
    }

    public class LabelInfo
    {
        [JsonProperty(PropertyName = "catalog-number")]
        public string catalognumber { get; set; }

        public Label label { get; set; }
    }

    public class LifeSpan
    {
        public string begin { get; set; }
        public string end { get; set; }
        public bool ended { get; set; }
    }

    public class Medium
    {
        public object format { get; set; }
        public int? position { get; set; }
        public string title { get; set; }

        [JsonProperty(PropertyName = "track-count")]
        public short? trackcount { get; set; }

        public int? trackoffset { get; set; }

        public List<Track> tracks { get; set; }
    }

    public class Recording
    {
        public List<Alias> aliases { get; set; }
        public string disambiguation { get; set; }
        public string id { get; set; }
        public int? length { get; set; }
        public string title { get; set; }
        public bool video { get; set; }
    }

    public class Relation
    {
        public List<object> attributes { get; set; }
        public AttributeValues attributevalues { get; set; }
        public object begin { get; set; }
        public string direction { get; set; }
        public object end { get; set; }
        public bool ended { get; set; }
        public string sourcecredit { get; set; }
        public string targetcredit { get; set; }
        public string targettype { get; set; }
        public string type { get; set; }
        public string typeid { get; set; }
        public Url url { get; set; }
    }

    [DebuggerDisplay("title: {title}, date: {date}")]
    public class Release
    {
        public List<object> aliases { get; set; }
        public string asin { get; set; }
        public string barcode { get; set; }
        public string country { get; set; }

        [JsonProperty(PropertyName = "cover-art-archive")]
        public CoverArtArchive coverartarchive { get; set; }

        public string coverThumbnailUrl { get; set; }
        public string date { get; set; }
        public string disambiguation { get; set; }
        public string id { get; set; }
        public List<string> imageUrls { get; set; }

        [JsonProperty(PropertyName = "label-info")]
        public List<LabelInfo> labelinfo { get; set; }

        public List<Medium> media { get; set; }
        public string packaging { get; set; }
        public string quality { get; set; }
        public List<Relation> relations { get; set; }

        [JsonProperty(PropertyName = "release-events")]
        public List<ReleaseEvents> releaseevents { get; set; }

        public ReleaseGroup releasegroup { get; set; }
        public string status { get; set; }
        public TextRepresentation textrepresentation { get; set; }
        public string title { get; set; }
    }

    public class ReleaseBrowseResult
    {
        [JsonProperty(PropertyName = "release-count")]
        public int? releasecount { get; set; }

        [JsonProperty(PropertyName = "release-offset")]
        public int? releaseoffset { get; set; }

        public List<Release> releases { get; set; }
    }

    public class ReleaseEvents
    {
        public Area area { get; set; }

        public string date { get; set; }
    }

    public class ReleaseGroup
    {
        public List<object> aliases { get; set; }
        public string disambiguation { get; set; }
        public string firstreleasedate { get; set; }
        public string id { get; set; }
        public string primarytype { get; set; }
        public List<object> secondarytypes { get; set; }
        public string title { get; set; }
    }

    public class Tag
    {
        public int? count { get; set; }

        public string name { get; set; }
    }

    public class TextRepresentation
    {
        public string language { get; set; }
        public string script { get; set; }
    }

    public class Track
    {
        public string id { get; set; }
        public int? length { get; set; }

        public string number { get; set; }
        public string position { get; set; }
        public Recording recording { get; set; }

        public string title { get; set; }
    }

    public class Url
    {
        public string id { get; set; }
        public string resource { get; set; }
    }
}