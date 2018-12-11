using CsvHelper;
using Newtonsoft.Json;
using Roadie.Library.Enums;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Roadie.Library.Data
{
    public partial class Collection
    {
        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:collection:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:collection_by_id:{ Id }";
        }

        public string CacheKey
        {
            get
            {
                return Collection.CacheUrn(this.RoadieId);
            }
        }

        public int? _positionColumn = null;
        public int PositionColumn
        {
            get
            {
                if (this._positionColumn == null)
                {
                    var looper = -1;
                    foreach (var pos in this.ListInCSVFormat.Split(','))
                    {
                        looper++;
                        if (pos.ToLower().Equals("position"))
                        {
                            this._positionColumn = looper;
                        }
                    }
                }
                return this._positionColumn.Value;
            }
        }

        public int? _artistColumn = null;
        public int ArtistColumn
        {
            get
            {
                if (this._artistColumn == null)
                {
                    var looper = -1;
                    foreach (var pos in this.ListInCSVFormat.Split(','))
                    {
                        looper++;
                        if (pos.ToLower().Equals("artist"))
                        {
                            this._artistColumn = looper;
                        }
                    }
                }
                return this._artistColumn.Value;
            }
        }

        public int? _releaseColumn = null;
        public int ReleaseColumn
        {
            get
            {
                if (this._releaseColumn == null)
                {
                    var looper = -1;
                    foreach (var pos in this.ListInCSVFormat.Split(','))
                    {
                        looper++;
                        if (pos.ToLower().Equals("release"))
                        {
                            this._releaseColumn = looper;
                        }
                    }
                }
                return this._releaseColumn.Value;
            }
        }


        private IEnumerable<PositionAristRelease> _positionAristReleases = null;

        public IEnumerable<PositionAristRelease> PositionArtistReleases()
        {
            if (this._positionAristReleases == null)
            {
                var rows = new List<PositionAristRelease>();
                using (var sr = new StringReader(this.ListInCSV))
                {
                    var index = 0;
                    var csv = new CsvReader(sr, new CsvHelper.Configuration.Configuration { MissingFieldFound = null, HasHeaderRecord = false });
                    while (csv.Read())
                    {
                        index++;
                        rows.Add(new PositionAristRelease
                        {
                            Index = index,
                            Position = csv.GetField<int>(this.PositionColumn),
                            Artist = SafeParser.ToString(csv.GetField<string>(this.ArtistColumn)),
                            Release = SafeParser.ToString(csv.GetField<string>(this.ReleaseColumn)),
                        });
                    }
                }
                this._positionAristReleases = rows;
            }
            return this._positionAristReleases;
        }
    }

    [Serializable]
    public class PositionAristRelease
    {
        [JsonIgnore]
        public Statuses Status { get; set; }

        [JsonProperty("Status")]
        public string StatusVerbose
        {
            get
            {
                return this.Status.ToString();
            }
        }
        /// <summary>
        /// This is the index (position in the list regardless of the position number)
        /// </summary>
        [JsonIgnore]
        public int Index { get; set; }
        /// <summary>
        /// This is the position number for the list (can be a year "1984" can be a number "14")
        /// </summary>
        public int Position { get; set; }
        public string Artist { get; set; }
        public string Release { get; set; }

        public override string ToString()
        {
            return string.Format("Position [{0}], Artist [{1}], Release [{2}]", this.Position, this.Artist, this.Release);
        }
    }
}
