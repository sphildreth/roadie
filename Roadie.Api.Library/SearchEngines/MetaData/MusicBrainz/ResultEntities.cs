using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Roadie.Library.MetaData.MusicBrainz
{
    [Serializable]
    public class ArtistResult
    {
        public Artist[] artists { get; set; }

        public int count { get; set; }

        public DateTime created { get; set; }

        public int offset { get; set; }
    }

    [Serializable]
    public class RepositoryArtist
    {
        public Artist Artist { get; set; }

        public string ArtistMbId { get; set; }

        public string ArtistName { get; set; }

        public int Id { get; set; }
    }

    [DebuggerDisplay("name: {name}")]
    [Serializable]
    public class Artist
    {
        public Alias[] aliases { get; set; }

        public Area area { get; set; }

        [JsonPropertyName("begin_area")]
        public BeginAndEndArea beginarea { get; set; }

        public string country { get; set; }

        [JsonPropertyName("end_area")]
        public BeginAndEndArea endarea { get; set; }

        public string gender { get; set; }

        public NameAndCount[] genres { get; set; }

        public string id { get; set; }

        public string[] ipis { get; set; }

        public IsniList[] isnilist { get; set; }

        [JsonPropertyName("life-span")]
        public LifeSpan lifespan { get; set; }

        public string name { get; set; }

        public Relation[] relations { get; set; }

        public int score { get; set; }

        [JsonPropertyName("sort-name")]
        public string sortname { get; set; }

        public NameAndCount[] tags { get; set; }

        public string type { get; set; }

        [JsonPropertyName("type-id")]
        public string typeid { get; set; }
    }

    [Serializable]
    public class Relations
    {
        [JsonPropertyName("attribute-ids")]
        public string[] attributeids { get; set; }

        public string[] attributes { get; set; }

        public string[] attributevalues { get; set; }

        public object begin { get; set; }

        public string direction { get; set; }

        public object end { get; set; }

        public bool? ended { get; set; }

        [JsonPropertyName("source-credit")]
        public string sourcecredit { get; set; }

        [JsonPropertyName("target-credit")]
        public string targetcredit { get; set; }

        [JsonPropertyName("target-type")]
        public string targettype { get; set; }

        public string type { get; set; }

        [JsonPropertyName("type-id")]
        public string typeid { get; set; }

        public Url url { get; set; }
    }

    [Serializable]
    public class Url
    {
        public string id { get; set; }

        public string resource { get; set; }
    }

    [Serializable]
    public class Area
    {
        public string id { get; set; }

        [JsonPropertyName("iso-3166-1-codes")]
        public string[] iso31661codes { get; set; }

        [JsonPropertyName("life-span")]
        public LifeSpan lifespan { get; set; }

        public string name { get; set; }

        [JsonPropertyName("sort-name")]
        public string sortname { get; set; }

        public string type { get; set; }

        [JsonPropertyName("type-id")]
        public string typeid { get; set; }
    }

    [Serializable]
    public class LifeSpan
    {
        public string begin { get; set; }

        public string end { get; set; }

        public bool? ended { get; set; }
    }

    [Serializable]
    public class BeginAndEndArea
    {
        public string id { get; set; }

        [JsonPropertyName("life-span")]
        public LifeSpan lifespan { get; set; }

        public string name { get; set; }

        [JsonPropertyName("sort-name")]
        public string sortname { get; set; }

        public string type { get; set; }

        public string typeid { get; set; }
    }

    [Serializable]
    public class Alias
    {
        public string begin { get; set; }

        public string end { get; set; }

        public bool? ended { get; set; }

        public string locale { get; set; }

        public string name { get; set; }

        public bool? primary { get; set; }

        [JsonPropertyName("sort-name")]
        public string sortname { get; set; }

        public string type { get; set; }

        [JsonPropertyName("type-id")]
        public string typeid { get; set; }
    }

    [Serializable]
    public class IsniList
    {
        public string isni { get; set; }
    }

    [Serializable]
    public class NameAndCount
    {
        public int? count { get; set; }

        public string name { get; set; }
    }
}
