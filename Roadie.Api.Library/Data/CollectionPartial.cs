using CsvHelper;
using Newtonsoft.Json;
using Roadie.Library.Configuration;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Roadie.Library.Data
{
    public partial class Collection
    {
        public int? _artistColumn;

        public int? _positionColumn;
        public int? _releaseColumn;
        private IEnumerable<PositionArtistRelease> _positionArtistReleases;

        public int ArtistColumn
        {
            get
            {
                if (_artistColumn == null)
                {
                    var looper = -1;
                    foreach (var pos in ListInCSVFormat.Split(','))
                    {
                        looper++;
                        if (pos.ToLower().Equals("artist")) _artistColumn = looper;
                    }
                }

                return _artistColumn.Value;
            }
        }

        public string CacheKey => CacheUrn(RoadieId);

        public string CacheRegion => CacheRegionUrn(RoadieId);

        /// <summary>
        ///     Returns a full file path to the Collection Image
        /// </summary>
        public string PathToImage(IRoadieSettings configuration, bool makeFolderIfNotExist = false)
        {
            var folder = configuration.CollectionImageFolder;
            if (!Directory.Exists(folder) && makeFolderIfNotExist)
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, $"{ (SortName ?? Name).ToFileNameFriendly() } [{ Id }].jpg");
        }

        public int PositionColumn
        {
            get
            {
                if (_positionColumn == null)
                {
                    var looper = -1;
                    foreach (var pos in ListInCSVFormat.Split(','))
                    {
                        looper++;
                        if (pos.ToLower().Equals("position")) _positionColumn = looper;
                    }
                }

                return _positionColumn.Value;
            }
        }

        public int ReleaseColumn
        {
            get
            {
                if (_releaseColumn == null)
                {
                    var looper = -1;
                    foreach (var pos in ListInCSVFormat.Split(','))
                    {
                        looper++;
                        if (pos.ToLower().Equals("release")) _releaseColumn = looper;
                    }
                }

                return _releaseColumn.Value;
            }
        }

        public Collection()
        {
            Releases = new HashSet<CollectionRelease>();
            Comments = new HashSet<Comment>();
            ListInCSVFormat = "Position,Release,Artist";
            CollectionType = Enums.CollectionType.Rank;
        }

        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:collection:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:collection_by_id:{Id}";
        }

        public IEnumerable<PositionArtistRelease> PositionArtistReleases()
        {
            if (_positionArtistReleases == null)
            {
                var rows = new List<PositionArtistRelease>();
                using (var sr = new StringReader(ListInCSV))
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
                            Position = csv.GetField<int>(PositionColumn),
                            Artist = SafeParser.ToString(csv.GetField<string>(ArtistColumn)),
                            Release = SafeParser.ToString(csv.GetField<string>(ReleaseColumn))
                        });
                    }
                }

                _positionArtistReleases = rows;
            }

            return _positionArtistReleases;
        }

        public override string ToString()
        {
            return $"Id [{Id}], Name [{Name}], RoadieId [{RoadieId}]";
        }
    }

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
        [JsonIgnore] public Statuses Status { get; set; }

        [JsonProperty("Status")] public string StatusVerbose => Status.ToString();

        public override string ToString()
        {
            return string.Format("Position [{0}], Artist [{1}], Release [{2}]", Position, Artist, Release);
        }
    }
}