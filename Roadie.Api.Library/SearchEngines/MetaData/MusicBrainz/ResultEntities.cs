using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Roadie.Library.MetaData.MusicBrainz
{
    [Serializable]
    public class ArtistResult
    {
        public DateTime created { get; set; }
        public int count { get; set; }
        public int offset { get; set; }
        public Artist[] artists { get; set; }
    }

    [Serializable]
    public class RepositoryArtist
    {
        public int Id { get; set; }
        public string ArtistName { get; set; }
        public string ArtistMbId { get; set; }
        public Artist Artist { get; set; }
    }

    [DebuggerDisplay("name: {name}")]
    [Serializable]
    public class Artist
    {
        public string id { get; set; }
        public string type { get; set; }
        [JsonProperty("type-id")]
        public string typeid { get; set; }
        public int score { get; set; }
        public string name { get; set; }
        [JsonProperty("sort-name")]
        public string sortname { get; set; }
        public string gender { get; set; }
        public string country { get; set; }
        public Area area { get; set; }
        [JsonProperty("begin_area")]
        public BeginAndEndArea beginarea { get; set; }
        [JsonProperty("end_area")]
        public BeginAndEndArea endarea { get; set; }
        public string[] ipis { get; set; }
        [JsonProperty("life-span")]
        public LifeSpan lifespan { get; set; }
        public Alias[] aliases { get; set; }
        public NameAndCount[] tags { get; set; }
        public IsniList[] isnilist { get; set; }
        public NameAndCount[] genres { get; set; }
        public Relation[] relations { get; set; }
    }

    [Serializable]
    public class Relations
    {
        [JsonProperty("attribute-ids")]
        public string[] attributeids { get; set; }
        public string[] attributes { get; set; }
        public string direction { get; set; }
        [JsonProperty("target-credit")]
        public string targetcredit { get; set; }
        [JsonProperty("type-id")]
        public string typeid { get; set; }
        [JsonProperty("target-type")]
        public string targettype { get; set; }
        public string type { get; set; }
        public bool ended { get; set; }
        public Url url { get; set; }
        public string[] attributevalues { get; set; }
        [JsonProperty("source-credit")]
        public string sourcecredit { get; set; }
        public object end { get; set; }
        public object begin { get; set; }
    }

    [Serializable]
    public class Url
    {
        public string resource { get; set; }
        public string id { get; set; }
    }


    [Serializable]
    public class Area
    {
        public string id { get; set; }
        public string type { get; set; }
        [JsonProperty("type-id")]
        public string typeid { get; set; }
        public string name { get; set; }
        [JsonProperty("sort-name")]
        public string sortname { get; set; }
        [JsonProperty("life-span")]
        public LifeSpan lifespan { get; set; }
        [JsonProperty("iso-3166-1-codes")]
        public string[] iso31661codes { get; set; }
    }

    [Serializable]
    public class LifeSpan
    {
        public string end { get; set; }
        public string ended { get; set; }
        public string begin { get; set; }
    }

    [Serializable]
    public class BeginAndEndArea
    {
        public string id { get; set; }
        public string type { get; set; }
        public string typeid { get; set; }
        public string name { get; set; }
        [JsonProperty("sort-name")]
        public string sortname { get; set; }
        [JsonProperty("life-span")]
        public LifeSpan lifespan { get; set; }
    }

    [Serializable]
    public class Alias
    {
        [JsonProperty("sort-name")]
        public string sortname { get; set; }
        [JsonProperty("type-id")]
        public string typeid { get; set; }
        public string name { get; set; }
        public string locale { get; set; }
        public string type { get; set; }
        public string primary { get; set; }
        public string begin { get; set; }
        public string end { get; set; }
        public bool ended { get; set; }
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
