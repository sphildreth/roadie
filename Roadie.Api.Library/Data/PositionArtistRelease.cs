using Roadie.Library.Enums;
using System;
using System.Text.Json.Serialization;

namespace Roadie.Library.Data
{
    [Serializable]
    public class PositionArtistRelease
    {
        public string Artist { get; set; }

        /// <summary>
        ///     This is the index (position in the list regardless of the position number)
        /// </summary>
        [JsonIgnore]
        public int Index { get; set; }

        /// <summary>
        ///     This is the position number for the list (can be a year "1984" can be a number "14")
        /// </summary>
        public int Position { get; set; }

        public string Release { get; set; }

        [JsonIgnore]
        public Statuses Status { get; set; }

        [JsonPropertyName("Status")]
        public string StatusVerbose => Status.ToString();

        public override string ToString() => string.Format("Position [{0}], Artist [{1}], Release [{2}]", Position, Artist, Release);
    }
}