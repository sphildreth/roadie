﻿using CsvHelper;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Roadie.Library.Data
{
    public partial class Collection
    {
        public const string ArtistPosition = "artist";
        public const string PositionPosition = "position";
        public const string ReleasePosition = "release";

        /// <summary>
        /// If the given value in either Artist or Release starts with this then the next value is the database Id, example "1,~4,~19"
        /// </summary>
        public static string DatabaseIdKey = "~";

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
                        if (String.Equals(pos, ArtistPosition, StringComparison.OrdinalIgnoreCase))
                        {
                            _artistColumn = looper;
                        }
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
                        if (String.Equals(pos, PositionPosition, StringComparison.OrdinalIgnoreCase))
                        {
                            _positionColumn = looper;
                        }
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
                        if (String.Equals(pos, ReleasePosition, StringComparison.OrdinalIgnoreCase))
                        {
                            _releaseColumn = looper;
                        }
                    }
                }

                return _releaseColumn.Value;
            }
        }

        public Collection()
        {
            Releases = new HashSet<CollectionRelease>();
            MissingReleases = new HashSet<CollectionMissing>();
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
                    var configuration = new CsvHelper.Configuration.CsvConfiguration(new CultureInfo("en-US", false))
                    {
                        MissingFieldFound = null,
                        HasHeaderRecord = false
                    };
                    configuration.BadDataFound = context => Trace.WriteLine($"PositionArtistReleases: Bad data found on row '{ context.Context.Parser.RawRow}'", "Warning");
                    using (var csv = new CsvReader(sr, configuration))
                    {
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
}