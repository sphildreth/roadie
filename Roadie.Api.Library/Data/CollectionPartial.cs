using CsvHelper;
using Newtonsoft.Json;
using Roadie.Library.Enums;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public string CacheRegion
        {
            get
            {
                return Collection.CacheRegionUrn(this.RoadieId);
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


        private IEnumerable<PositionArtistRelease> _positionArtistReleases = null;

        public IEnumerable<PositionArtistRelease> PositionArtistReleases()
        {
            if (this._positionArtistReleases == null)
            {
                var rows = new List<PositionArtistRelease>();
                using (var sr = new StringReader(this.ListInCSV))
                {
                    var index = 0;
                    var configuration = new CsvHelper.Configuration.Configuration
                    {
                        MissingFieldFound = null,
                        HasHeaderRecord = false
                    };
                    configuration.BadDataFound = context =>
                    {
                        Trace.WriteLine($"PositionArtistReleases: Bad data found on row '{context.RawRow}'");
                    };
                    var csv = new CsvReader(sr, configuration);
                    while (csv.Read())
                    {
                        index++;
                        rows.Add(new PositionArtistRelease
                        {
                            Index = index,
                            Position = csv.GetField<int>(this.PositionColumn),
                            Artist = SafeParser.ToString(csv.GetField<string>(this.ArtistColumn)),
                            Release = SafeParser.ToString(csv.GetField<string>(this.ReleaseColumn)),
                        });
                    }
                }
                this._positionArtistReleases = rows;
            }
            return this._positionArtistReleases;
        }

        public Collection()
        {
            this.Releases = new HashSet<CollectionRelease>();
            this.Comments = new HashSet<Comment>();
            ListInCSVFormat = "Position,Release,Artist";
            CollectionType = Enums.CollectionType.Rank;
        }

        public override string ToString()
        {
            return $"Id [{ this.Id }], Name [{ this.Name }]";
        }
    }

    [Serializable]
    public class PositionArtistRelease
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
