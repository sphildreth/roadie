using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.SearchEngines.MetaData;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.MetaData.MusicBrainz
{
    public sealed class MusicBrainzProvider : MetaDataProviderBase, IMusicBrainzProvider
    {
        public override bool IsEnabled => Configuration.Integrations.MusicBrainzProviderEnabled;
        private MusicBrainzRepository Repository { get; }

        public MusicBrainzProvider(IRoadieSettings configuration, ICacheManager cacheManager, ILogger<MusicBrainzProvider> logger)
            : base(configuration, cacheManager, logger)
        {
            Repository = new MusicBrainzRepository(configuration, logger);
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
                {
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
                }

                return result;
            }
            catch (Exception)
            {
            }

            return null;
        }

        public async Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearch(string query, int resultsCount)
        {
            ArtistSearchResult result = null;

            Logger.LogTrace("MusicBrainzProvider:PerformArtistSearch:{0}", query);
            var a = await Repository.ArtistByName(query, resultsCount);
            if (a != null)
            {
                var imageUrls = a.relations?.Where(x => x.type.Equals("image", StringComparison.OrdinalIgnoreCase)).Select(x => x.url.resource).Distinct().ToArray();
                var notImageUrls = a.relations?.Where(x => !x.type.Equals("image", StringComparison.OrdinalIgnoreCase)).Select(x => x.url.resource).Distinct().ToArray();
                var discogRelation = a.relations?.FirstOrDefault(x => x.type.Equals("discogs", StringComparison.OrdinalIgnoreCase));
                var discogId = discogRelation?.url?.resource?.LastSegmentInUrl();
                var amgRelation = a.relations?.FirstOrDefault(x => x.type.Equals("allmusic", StringComparison.OrdinalIgnoreCase));
                var amgId = amgRelation?.url?.resource?.LastSegmentInUrl();
                var lastFmRelation = a.relations?.FirstOrDefault(x => x.type.Equals("last.fm", StringComparison.OrdinalIgnoreCase));
                var lastFmId = lastFmRelation?.url?.resource?.LastSegmentInUrl();
                var iTunesRelation = a.relations?.FirstOrDefault(x => x.url?.resource?.StartsWith("https://itunes.apple.com/") ?? false);
                var iTunesId = iTunesRelation?.url?.resource?.LastSegmentInUrl();
                var spotifyRelation = a.relations?.FirstOrDefault(x => x.url?.resource?.StartsWith("https://open.spotify.com/artist/") ?? false);
                var spotifyId = spotifyRelation?.url?.resource?.LastSegmentInUrl();
                result = new ArtistSearchResult
                {
                    ArtistName = a.name,
                    ArtistSortName = a.sortname,
                    MusicBrainzId = a.id,
                    ArtistType = a.type,
                    ArtistGenres = a.genres?.Select(x => x.name).ToArray(),
                    AlternateNames = a.aliases?.Select(x => x.name).Distinct().ToArray(),
                    ArtistRealName = a.aliases?.FirstOrDefault(x => x.type == "Legal name")?.name,
                    BeginDate = (a.type ?? string.Empty) == "group" ? SafeParser.ToDateTime(a.lifespan?.begin) : null,
                    BirthDate = (a.type ?? string.Empty) == "group" ? null : SafeParser.ToDateTime(a.lifespan?.begin),
                    ImageUrls = imageUrls,
                    Urls = notImageUrls,
                    LastFMId = lastFmId,
                    AmgId = amgId,
                    iTunesId = iTunesId,
                    DiscogsId = discogId,
                    SpotifyId = spotifyId,
                    EndDate = SafeParser.ToDateTime(a.lifespan?.end),
                    Tags = a.tags?.Select(x => x.name).Distinct().ToArray(),
                    IPIs = a.ipis,
                    ISNIs = a.isnilist?.Select(x => x.isni).ToArray()
                };
                Logger.LogTrace($"MusicBrainzArtist: ArtistName [{ query }], MbId [{ result.MusicBrainzId }], DiscogId [{ result.DiscogsId }], LastFMId [{ result.LastFMId }], AmgId [{ result.AmgId }], Itunes [{ result.iTunesId }], Spotify [{ result.SpotifyId }]");
            }
            else
            {
                Logger.LogWarning("MusicBrainzArtist: ArtistName [{0}], No MusicBrainz Artist Found", query);
            }
            return new OperationResult<IEnumerable<ArtistSearchResult>>
            {
                IsSuccess = result != null,
                Data = new[] { result }
            };
        }

        public async Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName, string query, int resultsCount)
        {
            ReleaseSearchResult result = null;
            try
            {
                var releaseInfosForArtist = await ReleasesForArtist(artistName);
                if (releaseInfosForArtist != null)
                {
                    var r = releaseInfosForArtist.FirstOrDefault(x => x.title.Equals(query, StringComparison.OrdinalIgnoreCase));
                    if (r != null)
                    {
                        var imageUrls = r.relations?.Where(x => x.type.Equals("image", StringComparison.OrdinalIgnoreCase)).Select(x => x.url.resource).Distinct().ToArray();
                        var notImageUrls = r.relations?.Where(x => !x.type.Equals("image", StringComparison.OrdinalIgnoreCase)).Select(x => x.url.resource).Distinct().ToArray();
                        var discogRelation = r.relations?.FirstOrDefault(x => x.type.Equals("discogs", StringComparison.OrdinalIgnoreCase));
                        var discogId = discogRelation?.url?.resource?.LastSegmentInUrl();
                        var amgRelation = r.relations?.FirstOrDefault(x => x.type.Equals("allmusic", StringComparison.OrdinalIgnoreCase));
                        var amgId = amgRelation?.url?.resource?.LastSegmentInUrl();
                        var lastFmRelation = r.relations?.FirstOrDefault(x => x.type.Equals("last.fm", StringComparison.OrdinalIgnoreCase));
                        var lastFmId = lastFmRelation?.url?.resource?.LastSegmentInUrl();
                        var iTunesRelation = r.relations?.FirstOrDefault(x => x.url?.resource?.StartsWith("https://itunes.apple.com/") ?? false);
                        var iTunesId = iTunesRelation?.url?.resource?.LastSegmentInUrl();
                        var spotifyRelation = r.relations?.FirstOrDefault(x => x.url?.resource?.StartsWith("https://open.spotify.com/artist/") ?? false);
                        var spotifyId = spotifyRelation?.url?.resource?.LastSegmentInUrl();
                        result = new ReleaseSearchResult
                        {
                            ReleaseDate = r.releasegroup != null ? SafeParser.ToDateTime(r.releasegroup.firstreleasedate) : null,
                            ReleaseTitle = r.title,
                            ImageUrls = imageUrls,
                            Urls = notImageUrls,
                            LastFMId = lastFmId,
                            AmgId = amgId,
                            iTunesId = iTunesId,
                            DiscogsId = discogId,
                            SpotifyId = spotifyId,
                            MusicBrainzId = r.id,
                            Tags = r.releasegroup?.tags?.Select(x => x.name).Distinct().ToArray(),
                            ReleaseGenres = r.releasegroup?.genres?.Select(x => x.name).Distinct().ToArray(),
                            ReleaseType = r.releasegroup?.primarytype
                        };
                        var coverUrls = await CoverArtForMusicBrainzReleaseById(r.id);
                        if (coverUrls != null)
                        {
                            var frontCover = coverUrls.images.FirstOrDefault(i => i.front);
                            result.ImageUrls = coverUrls.images.Select(x => x.image).ToList();
                            if (frontCover != null)
                            {
                                result.ReleaseThumbnailUrl = frontCover.image;
                                result.ImageUrls = result.ImageUrls.Where(x => x != result.ReleaseThumbnailUrl).ToList();
                            }
                        }
                        if (r.labelinfo != null)
                        {
                            var releaseLabels = new List<ReleaseLabelSearchResult>();
                            foreach (var mbLabel in r.labelinfo)
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

                        if (r.media != null)
                        {
                            var releaseMedia = new List<ReleaseMediaSearchResult>();
                            foreach (var mbMedia in r.media.OrderBy(x => x.position))
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
                                        AlternateNames = mbTrack.recording != null && mbTrack.recording.aliases != null ? mbTrack.recording.aliases.Select(x => x.name).ToList() : null
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
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
            }

            if (result == null)
            {
                Logger.LogWarning("MusicBrainzArtist: ArtistName [{0}], ReleaseTitle [{0}], No MusicBrainz Release Found", artistName, query);
            }
            else
            {
                Logger.LogTrace("MusicBrainzArtist: Result [{0}]", query, result.ToString());
            }
            return new OperationResult<IEnumerable<ReleaseSearchResult>>
            {
                IsSuccess = result != null,
                Data = new[] { result }
            };
        }

        private async Task<CoverArtArchivesResult> CoverArtForMusicBrainzReleaseById(string musicBrainzId)
        {
            return await MusicBrainzRequestHelper.GetAsync<CoverArtArchivesResult>(MusicBrainzRequestHelper.CreateCoverArtReleaseUrl(musicBrainzId));
        }

        private async Task<IEnumerable<Release>> ReleasesForArtist(string artist, string artistMusicBrainzId = null)
        {
            if (string.IsNullOrEmpty(artistMusicBrainzId))
            {
                var artistSearch = await PerformArtistSearch(artist, 1);
                if (artistSearch == null || !artistSearch.IsSuccess)
                {
                    return null;
                }
                var mbArtist = artistSearch.Data.First();
                if (mbArtist == null)
                {
                    return null;
                }
                artistMusicBrainzId = mbArtist.MusicBrainzId;
            }
            return await Repository.ReleasesForArtist(artistMusicBrainzId);
        }
    }
}