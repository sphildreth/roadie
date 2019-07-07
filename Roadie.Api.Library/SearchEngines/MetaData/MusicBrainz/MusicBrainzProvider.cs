using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.SearchEngines.MetaData;
using Roadie.Library.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Roadie.Library.MetaData.MusicBrainz
{
    public class MusicBrainzProvider : MetaDataProviderBase, IMusicBrainzProvider
    {
        public override bool IsEnabled => Configuration.Integrations.MusicBrainzProviderEnabled;

        public MusicBrainzProvider(IRoadieSettings configuration, ICacheManager cacheManager, ILogger<MusicBrainzProvider> logger)
                    : base(configuration, cacheManager, logger)
        {
        }

        public async Task<CoverArtArchivesResult> CoverArtForMusicBrainzReleaseById(string musicBrainzId)
        {
            return await MusicBrainzRequestHelper.GetAsync<CoverArtArchivesResult>(MusicBrainzRequestHelper.CreateCoverArtReleaseUrl(musicBrainzId));
        }

        public async Task<Release> MusicBrainzReleaseById(string musicBrainzId)
        {
            if (string.IsNullOrEmpty(musicBrainzId)) return null;
            Release release = null;
            try
            {
                var artistCacheKey = string.Format("uri:musicbrainz:MusicBrainzReleaseById:{0}", musicBrainzId);
                release = CacheManager.Get<Release>(artistCacheKey);
                if (release == null)
                {
                    release = await MusicBrainzRequestHelper.GetAsync<Release>(
                        MusicBrainzRequestHelper.CreateLookupUrl("release", musicBrainzId,
                            "labels+aliases+recordings+release-groups+media+url-rels"));
                    if (release != null)
                    {
                        var coverUrls = await CoverArtForMusicBrainzReleaseById(musicBrainzId);
                        if (coverUrls != null)
                        {
                            var frontCover = coverUrls.images.FirstOrDefault(i => i.front);
                            release.imageUrls = coverUrls.images.Select(x => x.image).ToList();
                            if (frontCover != null)
                            {
                                release.coverThumbnailUrl = frontCover.image;
                                release.imageUrls = release.imageUrls.Where(x => x != release.coverThumbnailUrl)
                                    .ToList();
                            }
                        }

                        CacheManager.Add(artistCacheKey, release);
                    }
                }
            }
            catch (HttpRequestException)
            {
            }

            if (release == null)
                Logger.LogWarning("MusicBrainzReleaseById: MusicBrainzId [{0}], No MusicBrainz Release Found",
                    musicBrainzId);
            return release;
        }

        public async Task<IEnumerable<AudioMetaData>> MusicBrainzReleaseTracks(string artistName, string releaseTitle)
        {
            try
            {
                if (string.IsNullOrEmpty(artistName) && string.IsNullOrEmpty(releaseTitle)) return null;
                // Find the Artist
                var artistCacheKey = string.Format("uri:musicbrainz:artist:{0}", artistName);
                var artistSearch = await PerformArtistSearch(artistName, 1);
                if (!artistSearch.IsSuccess) return null;
                var artist = artistSearch.Data.First();

                if (artist == null) return null;
                var ReleaseCacheKey = string.Format("uri:musicbrainz:release:{0}", releaseTitle);
                var release = CacheManager.Get<Release>(ReleaseCacheKey);
                if (release == null)
                {
                    // Now Get Artist Details including Releases
                    var ReleaseResult = artist.Releases.FirstOrDefault(x =>
                        x.ReleaseTitle.Equals(releaseTitle, StringComparison.InvariantCultureIgnoreCase));
                    if (ReleaseResult == null)
                    {
                        ReleaseResult = artist.Releases.FirstOrDefault(x =>
                            x.ReleaseTitle.EndsWith(releaseTitle, StringComparison.InvariantCultureIgnoreCase));

                        if (ReleaseResult == null)
                        {
                            ReleaseResult = artist.Releases.FirstOrDefault(x =>
                                x.ReleaseTitle.StartsWith(releaseTitle, StringComparison.InvariantCultureIgnoreCase));
                            if (ReleaseResult == null) return null;
                        }
                    }

                    // Now get The Release Details
                    release = await MusicBrainzRequestHelper.GetAsync<Release>(
                        MusicBrainzRequestHelper.CreateLookupUrl("release", ReleaseResult.MusicBrainzId, "recordings"));
                    if (release == null) return null;
                    CacheManager.Add(ReleaseCacheKey, release);
                }

                var result = new List<AudioMetaData>();
                foreach (var media in release.media)
                    foreach (var track in media.tracks)
                    {
                        var date = 0;
                        if (!string.IsNullOrEmpty(release.date))
                        {
                            if (release.date.Length > 4)
                            {
                                var ReleaseDate = DateTime.MinValue;
                                if (DateTime.TryParse(release.date, out ReleaseDate)) date = ReleaseDate.Year;
                            }
                            else
                            {
                                int.TryParse(release.date, out date);
                            }
                        }

                        result.Add(new AudioMetaData
                        {
                            ReleaseMusicBrainzId = release.id,
                            MusicBrainzId = track.id,
                            Artist = artist.ArtistName,
                            Release = release.title,
                            Title = track.title,
                            Time = track.length.HasValue ? (TimeSpan?)TimeSpan.FromMilliseconds(track.length.Value) : null,
                            TrackNumber = SafeParser.ToNumber<short?>(track.position ?? track.number) ?? 0,
                            Disc = media.position,
                            Year = date > 0 ? (int?)date : null,
                            TotalTrackNumbers = media.trackcount
                            //tagFile.Tag.Pictures.Select(x => new AudoMetaDataImage
                            //{
                            //    Data = x.Data.Data,
                            //    Description = x.Description,
                            //    MimeType = x.MimeType,
                            //    Type = (AudioMetaDataImageType)x.Type
                            //}).ToArray()
                        });
                    }

                return result;
            }
            catch (Exception)
            {
            }

            return null;
        }

        public async Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearch(string query,
                                    int resultsCount)
        {
            ArtistSearchResult result = null;
            try
            {
                Logger.LogTrace("MusicBrainzProvider:PerformArtistSearch:{0}", query);
                // Find the Artist
                var artistCacheKey = string.Format("uri:musicbrainz:ArtistSearchResult:{0}", query);
                result = CacheManager.Get<ArtistSearchResult>(artistCacheKey);
                if (result == null)
                {
                    ArtistResult artistResult = null;
                    try
                    {
                        artistResult = await MusicBrainzRequestHelper.GetAsync<ArtistResult>(
                            MusicBrainzRequestHelper.CreateSearchTemplate("artist", query, resultsCount, 0));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex);
                    }

                    if (artistResult == null || artistResult.artists == null || artistResult.count < 1)
                        return new OperationResult<IEnumerable<ArtistSearchResult>>();
                    var a = artistResult.artists.First();
                    var mbArtist = await MusicBrainzRequestHelper.GetAsync<Artist>(
                        MusicBrainzRequestHelper.CreateLookupUrl("artist", artistResult.artists.First().id,
                            "releases"));
                    if (mbArtist == null) return new OperationResult<IEnumerable<ArtistSearchResult>>();
                    result = new ArtistSearchResult
                    {
                        ArtistName = mbArtist.name,
                        ArtistSortName = mbArtist.sortname,
                        MusicBrainzId = mbArtist.id,
                        ArtistType = mbArtist.type,
                        IPIs = mbArtist.ipis,
                        ISNIs = mbArtist.isnis
                    };
                    if (mbArtist.lifespan != null)
                    {
                        result.BeginDate = SafeParser.ToDateTime(mbArtist.lifespan.begin);
                        result.EndDate = SafeParser.ToDateTime(mbArtist.lifespan.end);
                    }

                    if (a.aliases != null) result.AlternateNames = a.aliases.Select(x => x.name).Distinct().ToArray();
                    if (a.tags != null) result.Tags = a.tags.Select(x => x.name).Distinct().ToArray();
                    var mbFilteredReleases = new List<Release>();
                    var filteredPlaces = new List<string> { "US", "WORLDWIDE", "XW", "GB" };
                    foreach (var release in mbArtist.releases)
                        if (filteredPlaces.Contains((release.country ?? string.Empty).ToUpper()))
                            mbFilteredReleases.Add(release);
                    result.Releases = new List<ReleaseSearchResult>();
                    var bag = new ConcurrentBag<Release>();
                    var filteredReleaseDetails = mbFilteredReleases.Select(async release =>
                    {
                        bag.Add(await MusicBrainzReleaseById(release.id));
                    });
                    await Task.WhenAll(filteredReleaseDetails);
                    foreach (var mbRelease in bag.Where(x => x != null))
                    {
                        var release = new ReleaseSearchResult
                        {
                            MusicBrainzId = mbRelease.id,
                            ReleaseTitle = mbRelease.title,
                            ReleaseThumbnailUrl = mbRelease.coverThumbnailUrl
                        };
                        if (mbRelease.imageUrls != null) release.ImageUrls = mbRelease.imageUrls;
                        if (mbRelease.releaseevents != null)
                            release.ReleaseDate = SafeParser.ToDateTime(mbRelease.releaseevents.First().date);
                        // Labels
                        if (mbRelease.media != null)
                        {
                            var releaseMedias = new List<ReleaseMediaSearchResult>();
                            foreach (var mbMedia in mbRelease.media)
                            {
                                var releaseMedia = new ReleaseMediaSearchResult
                                {
                                    ReleaseMediaNumber = SafeParser.ToNumber<short?>(mbMedia.position),
                                    TrackCount = mbMedia.trackcount
                                };
                                if (mbMedia.tracks != null)
                                {
                                    var releaseTracks = new List<TrackSearchResult>();
                                    foreach (var mbTrack in mbMedia.tracks)
                                        releaseTracks.Add(new TrackSearchResult
                                        {
                                            MusicBrainzId = mbTrack.id,
                                            TrackNumber = SafeParser.ToNumber<short?>(mbTrack.number),
                                            Title = mbTrack.title,
                                            Duration = mbTrack.length
                                        });
                                    releaseMedia.Tracks = releaseTracks;
                                }

                                releaseMedias.Add(releaseMedia);
                            }

                            release.ReleaseMedia = releaseMedias;
                        }

                        result.Releases.Add(release);
                    }

                    ;
                    CacheManager.Add(artistCacheKey, result);
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            if (result == null)
                Logger.LogWarning("MusicBrainzArtist: ArtistName [{0}], No MusicBrainz Artist Found", query);
            else
                Logger.LogTrace("MusicBrainzArtist: Result [{0}]", query, result.ToString());
            return new OperationResult<IEnumerable<ArtistSearchResult>>
            {
                IsSuccess = result != null,
                Data = new[] { result }
            };
        }

        public async Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName,
            string query, int resultsCount)
        {
            ReleaseSearchResult result = null;
            try
            {
                var releaseInfosForArtist = await ReleasesForArtist(artistName);
                if (releaseInfosForArtist != null)
                {
                    var releaseInfo = releaseInfosForArtist.FirstOrDefault(x =>
                        x.title.Equals(query, StringComparison.OrdinalIgnoreCase));
                    if (releaseInfo != null)
                    {
                        var mbRelease = await MusicBrainzReleaseById(releaseInfo.id);
                        if (mbRelease != null)
                        {
                            result = new ReleaseSearchResult
                            {
                                ReleaseDate = mbRelease.releasegroup != null
                                    ? SafeParser.ToDateTime(mbRelease.releasegroup.firstreleasedate)
                                    : null,
                                ReleaseTitle = mbRelease.title,
                                MusicBrainzId = mbRelease.id,
                                ReleaseType = mbRelease.releasegroup != null ? mbRelease.releasegroup.primarytype : null
                            };
                            if (mbRelease.labelinfo != null)
                            {
                                var releaseLabels = new List<ReleaseLabelSearchResult>();
                                foreach (var mbLabel in mbRelease.labelinfo)
                                    releaseLabels.Add(new ReleaseLabelSearchResult
                                    {
                                        CatalogNumber = mbLabel.catalognumber,
                                        Label = new LabelSearchResult
                                        {
                                            LabelName = mbLabel.label.name,
                                            MusicBrainzId = mbLabel.label.id,
                                            LabelSortName = mbLabel.label.sortname,
                                            AlternateNames = mbLabel.label.aliases.Select(x => x.name).ToList()
                                        }
                                    });
                                result.ReleaseLabel = releaseLabels;
                            }

                            if (mbRelease.media != null)
                            {
                                var releaseMedia = new List<ReleaseMediaSearchResult>();
                                foreach (var mbMedia in mbRelease.media.OrderBy(x => x.position))
                                {
                                    var mediaTracks = new List<TrackSearchResult>();
                                    short trackLooper = 0;
                                    foreach (var mbTrack in mbMedia.tracks.OrderBy(x => x.position))
                                    {
                                        trackLooper++;
                                        mediaTracks.Add(new TrackSearchResult
                                        {
                                            Title = mbTrack.title,
                                            TrackNumber = trackLooper,
                                            Duration = mbTrack.length,
                                            MusicBrainzId = mbTrack.id,
                                            AlternateNames =
                                                mbTrack.recording != null && mbTrack.recording.aliases != null
                                                    ? mbTrack.recording.aliases.Select(x => x.name).ToList()
                                                    : null
                                        });
                                    }

                                    releaseMedia.Add(new ReleaseMediaSearchResult
                                    {
                                        ReleaseMediaNumber = SafeParser.ToNumber<short?>(mbMedia.position),
                                        ReleaseMediaSubTitle = mbMedia.title,
                                        TrackCount = SafeParser.ToNumber<short?>(mbMedia.trackcount),
                                        Tracks = mediaTracks
                                    });
                                }

                                result.ReleaseMedia = releaseMedia;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
            }

            if (result == null)
                Logger.LogWarning(
                    "MusicBrainzArtist: ArtistName [{0}], ReleaseTitle [{0}], No MusicBrainz Release Found", artistName,
                    query);
            else
                Logger.LogTrace("MusicBrainzArtist: Result [{0}]", query, result.ToString());
            return new OperationResult<IEnumerable<ReleaseSearchResult>>
            {
                IsSuccess = result != null,
                Data = new[] { result }
            };
        }

        public async Task<Data.Release> ReleaseForMusicBrainzReleaseById(string musicBrainzId)
        {
            var release = await MusicBrainzReleaseById(musicBrainzId);
            if (release == null) return null;
            var media = release.media.First();
            if (media == null) return null;

            var result = new Data.Release
            {
                Title = release.title.ToTitleCase(false),
                ReleaseDate = SafeParser.ToDateTime(release.date),
                MusicBrainzId = release.id
            };

            var releaseMedia = new ReleaseMedia
            {
                Tracks = media.tracks.Select(m => new Data.Track
                {
                    TrackNumber = SafeParser.ToNumber<short>(m.position ?? m.number),
                    Title = m.title.ToTitleCase(false),
                    MusicBrainzId = m.id
                }).ToList()
            };
            result.Medias = new List<ReleaseMedia> { releaseMedia };
            return result;
        }

        public async Task<IEnumerable<Release>> ReleasesForArtist(string artist, string artistMusicBrainzId = null)
        {
            try
            {
                var artistSearch = await PerformArtistSearch(artist, 1);
                if (artistSearch == null || !artistSearch.IsSuccess) return null;
                var mbArtist = artistSearch.Data.First();
                if (string.IsNullOrEmpty(artistMusicBrainzId))
                {
                    if (mbArtist == null) return null;
                    artistMusicBrainzId = mbArtist.MusicBrainzId;
                }

                var cacheKey = string.Format("uri:musicbrainz:ReleasesForArtist:{0}", artistMusicBrainzId);
                var result = CacheManager.Get<List<Release>>(cacheKey);
                if (result == null)
                {
                    var pageSize = 50;
                    var page = 0;
                    var url = MusicBrainzRequestHelper.CreateArtistBrowseTemplate(artistMusicBrainzId, pageSize, 0);
                    var mbReleaseBrowseResult = await MusicBrainzRequestHelper.GetAsync<ReleaseBrowseResult>(url);
                    var totalReleases = mbReleaseBrowseResult != null ? mbReleaseBrowseResult.releasecount : 0;
                    var totalPages = Math.Ceiling((decimal)totalReleases / pageSize);
                    result = new List<Release>();
                    do
                    {
                        if (mbReleaseBrowseResult != null) result.AddRange(mbReleaseBrowseResult.releases);
                        page++;
                        mbReleaseBrowseResult = await MusicBrainzRequestHelper.GetAsync<ReleaseBrowseResult>(
                            MusicBrainzRequestHelper.CreateArtistBrowseTemplate(artistMusicBrainzId, pageSize,
                                pageSize * page));
                    } while (page < totalPages);

                    result = result.OrderBy(x => x.date).ThenBy(x => x.title).ToList();
                    CacheManager.Add(cacheKey, result);
                }

                return result;
            }
            catch (HttpRequestException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return null;
        }
    }
}