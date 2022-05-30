using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.Models;
using Roadie.Library.Models.Releases;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Roadie.Library.Inspect.Plugins.Directory
{
    public class RoadieDataFileCreator : FolderPluginBase
    {
        public static string RoadieDataFileCreatorDescription = "Creates Roadie JSON file(s) for the Directory.";

        public RoadieDataFileCreator(
            IRoadieSettings configuration,
            ICacheManager cacheManager,
            ILogger logger,
            IID3TagsHelper tagsHelper)
            : base(configuration, cacheManager, logger, tagsHelper)
        {
        }

        public override string Description => RoadieDataFileCreatorDescription;

        public override int Order => 999;

        public override OperationResult<string> Process(DirectoryInfo directory)
        {
            var result = new OperationResult<string>();
            var data = new StringBuilder();

            var stopWatch = Stopwatch.StartNew();

            // Get all MP3 Files in the given folder and group by Artist Name
            var mp3Files = directory.GetFiles("*.mp3", SearchOption.AllDirectories);

            var mp3TagDatas = new List<MetaData.Audio.AudioMetaData>();
            var invalidMp3sFound = new List<string>();
            foreach (var fileInfo in mp3Files)
            {
                var tagLib = TagsHelper.MetaDataForFile(fileInfo.FullName, true);
                if (!tagLib?.IsSuccess ?? false)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    data.AppendLine($"╟ ❗ INVALID: Missing: {ID3TagsHelper.DetermineMissingRequiredMetaData(tagLib.Data)}");
                    invalidMp3sFound.Add(fileInfo.FullName);
                }
                else
                {
                    mp3TagDatas.Add(tagLib.Data);
                }
            }

            if (invalidMp3sFound.Count > 0)
            {
                result.IsSuccess = false;
            }
            else
            {
                var now = DateTime.UtcNow;
                var mp3TagDatasGroupsByRelease = mp3TagDatas.GroupBy(x => x.Release);
                foreach (var mp3TagRelease in mp3TagDatasGroupsByRelease)
                {
                    DateTime? releaseDate = null;
                    var releaseYear = mp3TagRelease.Where(x => x.Year > 0).FirstOrDefault()?.Year;
                    if (releaseYear.HasValue && releaseYear > 0)
                    {
                        releaseDate = new DateTime(releaseYear.Value, 1, 1);
                    }
                    var releaseData = new ReleaseList<TrackListWithFileName>
                    {
                        Artist = new Models.DataToken
                        {
                            Value = Inspector.ToToken(mp3TagRelease.First().Artist),
                            Text = mp3TagRelease.First().Artist
                        },
                        Release = new Models.DataToken
                        {
                            Value = Inspector.ToToken(mp3TagRelease.Key),
                            Text = mp3TagRelease.Key
                        },
                        CreatedDate = now,
                        Id = Guid.NewGuid(),
                        MediaCount = mp3TagDatas.Select(x => x.Disc ?? 0).Distinct().Count(),
                        ReleaseDateDateTime = releaseDate,
                        Status = Enums.Statuses.New,
                        TrackCount = mp3TagRelease.Count()
                    };
                    var medias = new List<ReleaseMediaList<TrackListWithFileName>>();
                    foreach (var mp3TagData in mp3TagDatas.GroupBy(x => x.Disc))
                    {
                        var mediaTracks = mp3TagDatas.Where(x => x.Disc == mp3TagData.Key);
                        medias.Add(new ReleaseMediaList<TrackListWithFileName>
                        {
                            MediaNumber = SafeParser.ToNumber<short?>(mp3TagData.Key) ?? 1,
                            TrackCount = mediaTracks.Count(),
                            SubTitle = mp3TagData.First().DiscSubTitle,
                            Tracks = mediaTracks.OrderBy(x => x.TrackNumber).Select(x => new Models.TrackListWithFileName
                            {
                                CreatedDate = x.FileInfo.CreationTimeUtc,
                                Duration = SafeParser.ToNumber<int?>(x.Time?.TotalMilliseconds),
                                FileHash = HashHelper.GetHash(x.FileInfo.FullName).ToString(),
                                FileName = x.FileInfo.FullName,
                                FileSize = SafeParser.ToNumber<int?>(x.FileInfo.Length),
                                Id = Guid.NewGuid(),
                                Status = Enums.Statuses.New,
                                Title = x.Title,
                                TrackArtist = string.IsNullOrWhiteSpace(x.TrackArtist) || string.Equals(releaseData.Artist.Value, x.TrackArtist, StringComparison.OrdinalIgnoreCase) ? null : new Models.ArtistList
                                {
                                    Artist = new Models.DataToken
                                    {
                                        Value = Inspector.ToToken(x.TrackArtist),
                                        Text = x.TrackArtist
                                    }
                                },
                                TrackNumber = x.TrackNumber
                            }).ToArray()
                        });
                    }
                    releaseData.Media = medias;
                    releaseData.Status = releaseData.Media.SelectMany(x => x.Tracks).Count() == releaseData.Media.Sum(x => x.TrackCount) ? Enums.Statuses.Ok : Enums.Statuses.Incomplete;
                    releaseData.Duration = medias.SelectMany(x => x.Tracks).Sum(x => x.Duration);
                    var roadieDataFilenameByRelease = $"{releaseData.Artist.Text.ToFileNameFriendly()}.";
                    if (mp3TagDatasGroupsByRelease.Count() == 1)
                    {
                        roadieDataFilenameByRelease = null;
                    }
                    var roadieDataFileName = Path.Combine(directory.FullName, $"roadie.data.{roadieDataFilenameByRelease}json");
                    System.IO.File.WriteAllText(roadieDataFileName, CacheManager.CacheSerializer.Serialize(releaseData));
                    stopWatch.Stop();
                    data.Append($"Created [{roadieDataFileName}] data json file, elapsed Time [{ stopWatch.ElapsedMilliseconds }].");
                }
                result.IsSuccess = true;
            }
            result.Data = data.ToString();
            return result;
        }
    }
}
