﻿using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.SearchEngines.Imaging;
using Roadie.Library.SearchEngines.MetaData;
using Roadie.Library.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using discogs = Roadie.Library.SearchEngines.MetaData.Discogs;
using lastfm = Roadie.Library.MetaData.LastFm;
using musicbrainz = Roadie.Library.MetaData.MusicBrainz;
using spotify = Roadie.Library.SearchEngines.MetaData.Spotify;
using wikipedia = Roadie.Library.SearchEngines.MetaData.Wikipedia;

namespace Roadie.Library.Engines
{
    public class ReleaseLookupEngine : LookupEngineBase, IReleaseLookupEngine
    {
        public List<int> _addedReleaseIds = new List<int>();
        public List<int> _addedTrackIds = new List<int>();

        public IEnumerable<int> AddedReleaseIds => _addedReleaseIds;

        public IEnumerable<int> AddedTrackIds => _addedTrackIds;

        public IReleaseSearchEngine DiscogsReleaseSearchEngine { get; }

        public IReleaseSearchEngine ITunesReleaseSearchEngine { get; }

        public IReleaseSearchEngine LastFmReleaseSearchEngine { get; }

        public IReleaseSearchEngine MusicBrainzReleaseSearchEngine { get; }

        public IReleaseSearchEngine SpotifyReleaseSearchEngine { get; }

        public IReleaseSearchEngine WikipediaReleaseSearchEngine { get; }

        private IArtistLookupEngine ArtistLookupEngine { get; }

        private ILabelLookupEngine LabelLookupEngine { get; }

        public ReleaseLookupEngine(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context,
                                   ICacheManager cacheManager, ILogger<ReleaseLookupEngine> logger, IArtistLookupEngine artistLookupEngine,
                                   ILabelLookupEngine labelLookupEngine, musicbrainz.IMusicBrainzProvider musicBrainzProvider, lastfm.ILastFmHelper lastFmHelper,
                                   spotify.ISpotifyHelper spotifyHelper, wikipedia.IWikipediaHelper wikipediaHelper, discogs.IDiscogsHelper discogsHelper,
                                   IITunesSearchEngine iTunesSearchEngine)
            : base(configuration, httpEncoder, context, cacheManager, logger)
        {
            ArtistLookupEngine = artistLookupEngine;
            LabelLookupEngine = labelLookupEngine;

            ITunesReleaseSearchEngine = iTunesSearchEngine;
            MusicBrainzReleaseSearchEngine = musicBrainzProvider;
            LastFmReleaseSearchEngine = lastFmHelper;
            DiscogsReleaseSearchEngine = discogsHelper;
            SpotifyReleaseSearchEngine = spotifyHelper;
            WikipediaReleaseSearchEngine = wikipediaHelper;
        }

        public async Task<OperationResult<Release>> Add(Release release, bool doAddTracksInDatabase = false)
        {
            var sw = Stopwatch.StartNew();

            SimpleContract.Requires<ArgumentNullException>(release != null, "Invalid Release");

            try
            {
                var releaseGenreTables = release.Genres;
                var releaseMedias = release.Medias;
                var releaseLabels = release.Labels;
                var now = DateTime.UtcNow;
                release.AlternateNames = release.AlternateNames.AddToDelimitedList(new[] { release.Title.ToAlphanumericName() });
                release.Labels = null;
                release.Medias = null;
                release.Genres = null;
                release.LibraryStatus = LibraryStatus.Incomplete;
                release.Status = Statuses.New;
                var releaseImages = new List<Library.Imaging.Image>();
                if (!release.IsValid)
                {
                    return new OperationResult<Release>
                    {
                        Errors = new Exception[1] { new Exception("Release is Invalid") }
                    };
                }
                DbContext.Releases.Add(release);
                var inserted = 0;
                try
                {
                    inserted = await DbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Serialize());
                }

                if (inserted > 0 && release.Id > 0)
                {
                    _addedReleaseIds.Add(release.Id);
                    if (releaseGenreTables != null && releaseGenreTables.Any(x => x.GenreId == null))
                    {
                        var addedGenreIds = new List<int>();
                        foreach (var releaseGenreTable in releaseGenreTables)
                        {
                            var genreName = releaseGenreTable.Genre?.Name?.ToAlphanumericName();
                            if (string.IsNullOrEmpty(genreName)) continue;
                            if (releaseGenreTable.Genre.Name.Length > 100)
                            {
                                var originalName = releaseGenreTable.Genre.Name;
                                releaseGenreTable.Genre.Name = releaseGenreTable.Genre.Name.Substring(0, 99);
                                genreName = genreName.Substring(0, 99);
                                Logger.LogWarning($"Genre Name Too long was [{originalName}] truncated to [{releaseGenreTable.Genre.Name}]");
                            }
                            var genre = DbContext.Genres.FirstOrDefault(x => x.NormalizedName == genreName);
                            if (genre == null)
                            {
                                genre = new Genre
                                {
                                    Name = releaseGenreTable.Genre.Name,
                                    NormalizedName = genreName
                                };
                                DbContext.Genres.Add(genre);
                                await DbContext.SaveChangesAsync();
                            }
                            if (genre != null &&
                                genre.Id > 0 &&
                                !addedGenreIds.Any(x => x == genre.Id))
                            {
                                DbContext.ReleaseGenres.Add(new ReleaseGenre
                                {
                                    ReleaseId = release.Id,
                                    GenreId = genre.Id
                                });
                                addedGenreIds.Add(genre.Id);
                            }
                        }
                    }

                    // TODO #29 save release images to release folder 

                    //if (releaseImages != null && releaseImages.Any(x => x.Status == Statuses.New))
                    //{
                    //    foreach (var releaseImage in releaseImages)
                    //    {
                    //        DbContext.Images.Add(new Image
                    //        {
                    //            ReleaseId = release.Id,
                    //            Url = releaseImage.Url,
                    //            Signature = releaseImage.Signature,
                    //            Bytes = releaseImage.Bytes
                    //        });
                    //    }
                    //    try
                    //    {
                    //        await DbContext.SaveChangesAsync();
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Logger.LogError(ex);
                    //    }
                    //}

                    if (releaseLabels != null && releaseLabels.Any(x => x.Status == Statuses.New))
                    {
                        foreach (var neweleaseLabel in releaseLabels.Where(x => x.Status == Statuses.New))
                        {
                            var labelFetch = await LabelLookupEngine.GetByName(neweleaseLabel.Label.Name, true);
                            if (labelFetch.IsSuccess)
                                DbContext.ReleaseLabels.Add(new ReleaseLabel
                                {
                                    CatalogNumber = neweleaseLabel.CatalogNumber,
                                    BeginDate = neweleaseLabel.BeginDate,
                                    EndDate = neweleaseLabel.EndDate,
                                    ReleaseId = release.Id,
                                    LabelId = labelFetch.Data.Id
                                });
                        }

                        try
                        {
                            await DbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex);
                        }
                    }

                    if (doAddTracksInDatabase)
                    {
                        if (releaseMedias != null && releaseMedias.Any(x => x.Status == Statuses.New))
                        {
                            foreach (var newReleaseMedia in releaseMedias.Where(x => x.Status == Statuses.New))
                            {
                                var releasemedia = new ReleaseMedia
                                {
                                    Status = Statuses.Incomplete,
                                    MediaNumber = newReleaseMedia.MediaNumber,
                                    SubTitle = newReleaseMedia.SubTitle,
                                    TrackCount = newReleaseMedia.TrackCount,
                                    ReleaseId = release.Id
                                };
                                var releasemediatracks = new List<Track>();
                                foreach (var newTrack in newReleaseMedia.Tracks)
                                {
                                    int? trackArtistId = null;
                                    string partTitles = null;
                                    if (newTrack.TrackArtist != null)
                                    {
                                        if (!release.IsCastRecording)
                                        {
                                            var trackArtistData = await ArtistLookupEngine.GetByName(new AudioMetaData { Artist = newTrack.TrackArtist.Name }, true);
                                            if (trackArtistData.IsSuccess)
                                            {
                                                trackArtistId = trackArtistData.Data.Id;
                                            }
                                        }
                                        else if (newTrack.TrackArtists != null && newTrack.TrackArtists.Any())
                                        {
                                            partTitles = string.Join("/", newTrack.TrackArtists);
                                        }
                                        else
                                        {
                                            partTitles = newTrack.TrackArtist.Name;
                                        }
                                    }

                                    releasemediatracks.Add(new Track
                                    {
                                        ArtistId = trackArtistId,
                                        PartTitles = partTitles,
                                        Status = Statuses.Incomplete,
                                        TrackNumber = newTrack.TrackNumber,
                                        MusicBrainzId = newTrack.MusicBrainzId,
                                        SpotifyId = newTrack.SpotifyId,
                                        AmgId = newTrack.AmgId,
                                        Title = newTrack.Title,
                                        AlternateNames = newTrack.AlternateNames,
                                        Duration = newTrack.Duration,
                                        Tags = newTrack.Tags,
                                        ISRC = newTrack.ISRC,
                                        LastFMId = newTrack.LastFMId
                                    });
                                }

                                releasemedia.Tracks = releasemediatracks;
                                DbContext.ReleaseMedias.Add(releasemedia);
                                _addedTrackIds.AddRange(releasemedia.Tracks.Select(x => x.Id));
                            }

                            try
                            {
                                await DbContext.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex);
                            }
                        }
                    }
                    sw.Stop();
                    Logger.LogTrace($"Added New Release: Elapsed Time [{ sw.ElapsedMilliseconds }], Release `{ release }`");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
            }

            return new OperationResult<Release>
            {
                IsSuccess = release.Id > 0,
                Data = release,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<Release>> GetByName(Artist artist, AudioMetaData metaData,
                    bool doFindIfNotInDatabase = false, bool doAddTracksInDatabase = false, int? submissionId = null)
        {
            SimpleContract.Requires<ArgumentNullException>(artist != null, "Invalid Artist");
            SimpleContract.Requires<ArgumentOutOfRangeException>(artist.Id > 0, "Invalid Artist Id");
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                var cacheRegion = new Release { Artist = artist, Title = metaData.Release }.CacheRegion;
                var cacheKey = string.Format("urn:release_by_artist_id_and_name:{0}:{1}", artist.Id, metaData.Release);
                var resultInCache = CacheManager.Get<Release>(cacheKey, cacheRegion);
                if (resultInCache != null)
                {
                    sw.Stop();
                    return new OperationResult<Release>
                    {
                        IsSuccess = true,
                        OperationTime = sw.ElapsedMilliseconds,
                        Data = resultInCache
                    };
                }

                var searchName = metaData.Release.NormalizeName().ToLower();
                var specialSearchName = metaData.Release.ToAlphanumericName();

                var altStart = $"{searchName}|";
                var altIn = $"|{searchName}|";
                var altEnds = $"|{searchName}";

                var altStartSpecial = $"{specialSearchName}|";
                var altInSpecial = $"|{specialSearchName}|";
                var altEndsSpecial = $"|{specialSearchName}";

                var release = (from r in DbContext.Releases
                               where r.ArtistId == artist.Id
                               where r.Title == searchName ||
                                     r.Title == specialSearchName ||
                                     r.AlternateNames == searchName ||
                                     r.AlternateNames == specialSearchName ||
                                     r.AlternateNames.Contains(altStart) ||
                                     r.AlternateNames.Contains(altIn) ||
                                     r.AlternateNames.Contains(altEnds) ||
                                     r.AlternateNames.Contains(altStartSpecial) ||
                                     r.AlternateNames.Contains(altInSpecial) ||
                                     r.AlternateNames.Contains(altEndsSpecial)
                               select r
                    ).FirstOrDefault();

                sw.Stop();
                if (release == null || !release.IsValid)
                {
                    Logger.LogTrace("ReleaseFactory: Release Not Found For Artist `{0}` MetaData [{1}]",
                        artist.ToString(), metaData.ToString());
                    if (doFindIfNotInDatabase)
                    {
                        var releaseSearch = new OperationResult<Release>();
                        try
                        {
                            releaseSearch = await PerformMetaDataProvidersReleaseSearch(metaData,
                                artist.ArtistFileFolder(Configuration), submissionId);
                        }
                        catch (Exception ex)
                        {
                            sw.Stop();
                            Logger.LogError(ex);
                            return new OperationResult<Release>
                            {
                                OperationTime = sw.ElapsedMilliseconds,
                                Errors = new Exception[1] { ex }
                            };
                        }

                        if (releaseSearch.IsSuccess)
                        {
                            release = releaseSearch.Data;
                            release.ArtistId = artist.Id;
                            var addResult = await Add(release, doAddTracksInDatabase);
                            if (!addResult.IsSuccess)
                            {
                                sw.Stop();
                                return new OperationResult<Release>
                                {
                                    OperationTime = sw.ElapsedMilliseconds,
                                    Errors = addResult.Errors
                                };
                            }
                        }
                    }
                }

                if (release != null) CacheManager.Add(cacheKey, release);
                return new OperationResult<Release>
                {
                    IsSuccess = release != null,
                    OperationTime = sw.ElapsedMilliseconds,
                    Data = release
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return new OperationResult<Release>();
        }

        public async Task<OperationResult<Release>> PerformMetaDataProvidersReleaseSearch(AudioMetaData metaData,
            string artistFolder = null, int? submissionId = null)
        {
            SimpleContract.Requires<ArgumentNullException>(metaData != null, "Invalid MetaData");

            var sw = new Stopwatch();
            sw.Start();

            var result = new Release
            {
                Title = metaData.Release.ToTitleCase(false),
                TrackCount = (short)(metaData.TotalTrackNumbers ?? 0),
                ReleaseDate = SafeParser.ToDateTime(metaData.Year),
                SubmissionId = submissionId
            };
            var resultsExceptions = new List<Exception>();
            var releaseGenres = new List<string>();
            // Add any Genre found in the given MetaData
            if (metaData.Genres != null) releaseGenres.AddRange(metaData.Genres);
            var releaseLabels = new List<ReleaseLabelSearchResult>();
            var releaseMedias = new List<ReleaseMediaSearchResult>();
            var releaseImages = new List<Imaging.IImage>();
            var releaseImageUrls = new List<string>();

            var dontDoMetaDataProvidersSearchArtists = Configuration.DontDoMetaDataProvidersSearchArtists;
            if (!dontDoMetaDataProvidersSearchArtists.Any(x =>
                x.Equals(metaData.Artist, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    #region ITunes

                    if (ITunesReleaseSearchEngine.IsEnabled)
                    {
                        var sw2 = Stopwatch.StartNew();
                        Logger.LogTrace("ITunesReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var iTunesResult = await ITunesReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (iTunesResult.IsSuccess)
                        {
                            var i = iTunesResult.Data.First();
                            if (i.AlternateNames != null)
                                result.AlternateNames = result.AlternateNames.AddToDelimitedList(i.AlternateNames);
                            if (i.Tags != null) result.Tags = result.Tags.AddToDelimitedList(i.Tags);
                            if (i.Urls != null) result.URLs = result.URLs.AddToDelimitedList(i.Urls);
                            if (i.ImageUrls != null) releaseImageUrls.AddRange(i.ImageUrls);
                            if (i.ReleaseGenres != null) releaseGenres.AddRange(i.ReleaseGenres);
                            if(!string.IsNullOrEmpty(i.ReleaseThumbnailUrl))
                            {
                                releaseImages.Add(new Imaging.Image()
                                {
                                    Bytes = WebHelper.BytesForImageUrl(i.ReleaseThumbnailUrl)
                                });
                            }
                            result.CopyTo(new Release
                            {
                                ReleaseDate = result.ReleaseDate ?? i.ReleaseDate,
                                AmgId = i.AmgId,
                                Profile = i.Profile,
                                ITunesId = i.iTunesId,
                                Title = result.Title ?? i.ReleaseTitle,
                                ReleaseType = result.ReleaseType == ReleaseType.Unknown
                                    ? SafeParser.ToEnum<ReleaseType>(i.ReleaseType)
                                    : result.ReleaseType
                            });
                            if (i.ReleaseLabel != null) releaseLabels.AddRange(i.ReleaseLabel);
                            if (i.ReleaseMedia != null) releaseMedias.AddRange(i.ReleaseMedia);
                        }

                        if (iTunesResult.Errors != null) resultsExceptions.AddRange(iTunesResult.Errors);
                        sw2.Stop();
                        Logger.LogTrace($"PerformMetaDataProvidersReleaseSearch: ITunesArtistSearchEngine Complete [{ sw2.ElapsedMilliseconds }]");
                    }

                    #endregion ITunes

                    #region MusicBrainz

                    if (MusicBrainzReleaseSearchEngine.IsEnabled)
                    {
                        var sw2 = Stopwatch.StartNew();
                        Logger.LogTrace("MusicBrainzReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var mbResult = await MusicBrainzReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (mbResult.IsSuccess)
                        {
                            var mb = mbResult.Data.First();
                            if (mb.AlternateNames != null)
                                result.AlternateNames = result.AlternateNames.AddToDelimitedList(mb.AlternateNames);
                            if (mb.Tags != null) result.Tags = result.Tags.AddToDelimitedList(mb.Tags);
                            if (mb.Urls != null) result.URLs = result.URLs.AddToDelimitedList(mb.Urls);
                            if (mb.ImageUrls != null) releaseImageUrls.AddRange(mb.ImageUrls);
                            if (mb.ReleaseGenres != null) releaseGenres.AddRange(mb.ReleaseGenres);
                            if (!string.IsNullOrEmpty(mb.ReleaseTitle) &&
                                !mb.ReleaseTitle.Equals(result.Title, StringComparison.OrdinalIgnoreCase))
                                result.AlternateNames.AddToDelimitedList(new[] { mb.ReleaseTitle });
                            if (!string.IsNullOrEmpty(mb.ReleaseThumbnailUrl))
                            {
                                releaseImages.Add(new Imaging.Image()
                                {
                                    Bytes = WebHelper.BytesForImageUrl(mb.ReleaseThumbnailUrl)
                                });
                            }
                            result.CopyTo(new Release
                            {
                                ReleaseDate = result.ReleaseDate ?? mb.ReleaseDate,
                                AmgId = mb.AmgId,
                                Profile = mb.Profile,
                                TrackCount = mb.ReleaseMedia != null
                                    ? (short)mb.ReleaseMedia.Sum(x => x.TrackCount)
                                    : (short)0,
                                MusicBrainzId = mb.MusicBrainzId,
                                ITunesId = mb.iTunesId,
                                Title = result.Title ?? mb.ReleaseTitle,
                                ReleaseType = result.ReleaseType == ReleaseType.Unknown
                                    ? SafeParser.ToEnum<ReleaseType>(mb.ReleaseType)
                                    : result.ReleaseType
                            });
                            if (mb.ReleaseLabel != null) releaseLabels.AddRange(mb.ReleaseLabel);
                            if (mb.ReleaseMedia != null) releaseMedias.AddRange(mb.ReleaseMedia);
                        }

                        if (mbResult.Errors != null) resultsExceptions.AddRange(mbResult.Errors);
                        sw2.Stop();
                        Logger.LogTrace($"PerformMetaDataProvidersReleaseSearch: MusicBrainzReleaseSearchEngine Complete [{ sw2.ElapsedMilliseconds }]");
                    }

                    #endregion MusicBrainz

                    #region LastFm

                    if (LastFmReleaseSearchEngine.IsEnabled)
                    {
                        var sw2 = Stopwatch.StartNew();
                        Logger.LogTrace("LastFmReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var lastFmResult =
                            await LastFmReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (lastFmResult.IsSuccess)
                        {
                            var l = lastFmResult.Data.First();
                            if (l.AlternateNames != null)
                                result.AlternateNames = result.AlternateNames.AddToDelimitedList(l.AlternateNames);
                            if (l.Tags != null) result.Tags = result.Tags.AddToDelimitedList(l.Tags);
                            if (l.Urls != null) result.URLs = result.URLs.AddToDelimitedList(l.Urls);
                            if (l.ImageUrls != null) releaseImageUrls.AddRange(l.ImageUrls);
                            if (l.ReleaseGenres != null) releaseGenres.AddRange(l.ReleaseGenres);
                            if (!string.IsNullOrEmpty(l.ReleaseTitle) &&
                                !l.ReleaseTitle.Equals(result.Title, StringComparison.OrdinalIgnoreCase))
                                result.AlternateNames.AddToDelimitedList(new[] { l.ReleaseTitle });
                            if (!string.IsNullOrEmpty(l.ReleaseThumbnailUrl))
                            {
                                releaseImages.Add(new Imaging.Image()
                                {
                                    Bytes = WebHelper.BytesForImageUrl(l.ReleaseThumbnailUrl)
                                });
                            }
                            result.CopyTo(new Release
                            {
                                ReleaseDate = result.ReleaseDate ?? l.ReleaseDate,
                                AmgId = l.AmgId,
                                Profile = l.Profile,
                                LastFMId = l.LastFMId,
                                LastFMSummary = l.LastFMSummary,
                                MusicBrainzId = l.MusicBrainzId,
                                ITunesId = l.iTunesId,
                                Title = result.Title ?? l.ReleaseTitle,
                                ReleaseType = result.ReleaseType == ReleaseType.Unknown
                                    ? SafeParser.ToEnum<ReleaseType>(l.ReleaseType)
                                    : result.ReleaseType
                            });
                            if (l.ReleaseLabel != null) releaseLabels.AddRange(l.ReleaseLabel);
                            if (l.ReleaseMedia != null) releaseMedias.AddRange(l.ReleaseMedia);
                        }

                        if (lastFmResult.Errors != null) resultsExceptions.AddRange(lastFmResult.Errors);
                        sw2.Stop();
                        Logger.LogTrace($"PerformMetaDataProvidersReleaseSearch: LastFmReleaseSearchEngine Complete [{ sw2.ElapsedMilliseconds }]");
                    }

                    #endregion LastFm

                    #region Spotify

                    if (SpotifyReleaseSearchEngine.IsEnabled)
                    {
                        var sw2 = Stopwatch.StartNew();
                        Logger.LogTrace("SpotifyReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var spotifyResult = await SpotifyReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (spotifyResult.IsSuccess)
                        {
                            var s = spotifyResult.Data.First();
                            if (s.Tags != null) result.Tags = result.Tags.AddToDelimitedList(s.Tags);
                            if (s.Urls != null) result.URLs = result.URLs.AddToDelimitedList(s.Urls);
                            if (s.ImageUrls != null) releaseImageUrls.AddRange(s.ImageUrls);
                            if (s.ReleaseGenres != null) releaseGenres.AddRange(s.ReleaseGenres);
                            if (!string.IsNullOrEmpty(s.ReleaseTitle) &&
                                !s.ReleaseTitle.Equals(result.Title, StringComparison.OrdinalIgnoreCase))
                                result.AlternateNames.AddToDelimitedList(new[] { s.ReleaseTitle });
                            if (!string.IsNullOrEmpty(s.ReleaseThumbnailUrl))
                            {
                                releaseImages.Add(new Imaging.Image()
                                {
                                    Bytes = WebHelper.BytesForImageUrl(s.ReleaseThumbnailUrl)
                                });
                            }
                            result.CopyTo(new Release
                            {
                                ReleaseDate = result.ReleaseDate ?? s.ReleaseDate,
                                AmgId = s.AmgId,
                                Profile = HttpEncoder.HtmlEncode(s.Profile),
                                SpotifyId = s.SpotifyId,
                                MusicBrainzId = s.MusicBrainzId,
                                ITunesId = s.iTunesId,
                                Title = result.Title ?? s.ReleaseTitle,
                                ReleaseType = result.ReleaseType == ReleaseType.Unknown
                                    ? SafeParser.ToEnum<ReleaseType>(s.ReleaseType)
                                    : result.ReleaseType
                            });
                            if (s.ReleaseLabel != null) releaseLabels.AddRange(s.ReleaseLabel);
                            if (s.ReleaseMedia != null) releaseMedias.AddRange(s.ReleaseMedia);
                        }

                        if (spotifyResult.Errors != null) resultsExceptions.AddRange(spotifyResult.Errors);
                        sw2.Stop();
                        Logger.LogTrace($"PerformMetaDataProvidersReleaseSearch: SpotifyReleaseSearchEngine Complete [{ sw2.ElapsedMilliseconds }]");
                    }

                    #endregion Spotify

                    #region Discogs

                    if (DiscogsReleaseSearchEngine.IsEnabled)
                    {
                        var sw2 = Stopwatch.StartNew();
                        Logger.LogTrace("DiscogsReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var discogsResult = await DiscogsReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (discogsResult.IsSuccess)
                        {
                            var d = discogsResult.Data.First();
                            if (d.Urls != null) result.URLs = result.URLs.AddToDelimitedList(d.Urls);
                            if (d.ImageUrls != null) releaseImageUrls.AddRange(d.ImageUrls);
                            if (d.AlternateNames != null)
                                result.AlternateNames = result.AlternateNames.AddToDelimitedList(d.AlternateNames);
                            if (!string.IsNullOrEmpty(d.ReleaseTitle) &&
                                !d.ReleaseTitle.Equals(result.Title, StringComparison.OrdinalIgnoreCase))
                                result.AlternateNames.AddToDelimitedList(new[] { d.ReleaseTitle });
                            if (!string.IsNullOrEmpty(d.ReleaseThumbnailUrl))
                            {
                                releaseImages.Add(new Imaging.Image()
                                {
                                    Bytes = WebHelper.BytesForImageUrl(d.ReleaseThumbnailUrl)
                                });
                            }
                            result.CopyTo(new Release
                            {
                                Profile = HttpEncoder.HtmlEncode(d.Profile),
                                DiscogsId = d.DiscogsId,
                                Title = result.Title ?? d.ReleaseTitle,
                                ReleaseType = result.ReleaseType == ReleaseType.Unknown
                                    ? SafeParser.ToEnum<ReleaseType>(d.ReleaseType)
                                    : result.ReleaseType
                            });
                            if (d.ReleaseLabel != null) releaseLabels.AddRange(d.ReleaseLabel);
                            if (d.ReleaseMedia != null) releaseMedias.AddRange(d.ReleaseMedia);
                        }

                        if (discogsResult.Errors != null) resultsExceptions.AddRange(discogsResult.Errors);
                        sw2.Stop();
                        Logger.LogTrace($"PerformMetaDataProvidersReleaseSearch: DiscogsReleaseSearchEngine Complete [{ sw2.ElapsedMilliseconds }]");
                    }

                    #endregion Discogs
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }

                Logger.LogTrace("Metadata Providers Search Complete. [{0}]", sw.ElapsedMilliseconds);
            }
            else
            {
                Logger.LogTrace("Skipped Metadata Providers Search, DontDoMetaDataProvidersSearchArtists set for Artist [{0}].", metaData.Artist);
            }

            if (result.AlternateNames != null)
            {
                result.AlternateNames = string.Join("|", result.AlternateNames.ToListFromDelimited().Distinct().OrderBy(x => x));
            }
            if (result.URLs != null)
            {
                result.URLs = string.Join("|", result.URLs.ToListFromDelimited().Distinct().OrderBy(x => x));
            }
            if (result.Tags != null)
            {
                result.Tags = string.Join("|", result.Tags.ToListFromDelimited().Distinct().OrderBy(x => x));
            }
            if (releaseGenres.Any())
            {
                var sw2 = Stopwatch.StartNew();
                result.Genres = new List<ReleaseGenre>();
                foreach (var releaseGenre in releaseGenres.Where(x => !string.IsNullOrEmpty(x)).GroupBy(x => x).Select(x => x.First()))
                {
                    var rg = releaseGenre.Trim();
                    if (!string.IsNullOrEmpty(rg))
                    {
                        foreach (var g in ID3TagsHelper.SplitGenre(rg))
                        {
                            result.Genres.Add(new ReleaseGenre
                            {
                                Genre = DbContext.Genres.Where(x => x.Name.ToLower() == g.ToLower()).FirstOrDefault() ?? new Genre
                                {
                                    Name = g,
                                    NormalizedName = g.ToAlphanumericName()
                                }
                            });
                        }
                    }
                };
                sw2.Stop();
                Logger.LogTrace($"PerformMetaDataProvidersReleaseSearch: Release Genre Processing Complete [{ sw2.ElapsedMilliseconds }]");
            }

            if (releaseImageUrls.Any())
            {
                var sw2 = Stopwatch.StartNew();
                var imageBag = new ConcurrentBag<IImage>();
                var i = releaseImageUrls.Select(async url =>
                {
                    imageBag.Add(await WebHelper.GetImageFromUrlAsync(url));
                });
                await Task.WhenAll(i);
                releaseImages.AddRange(imageBag.ToList());
                sw2.Stop();
                Logger.LogTrace($"PerformMetaDataProvidersReleaseSearch: Image Url Processing Complete [{ sw2.ElapsedMilliseconds }]");
            }

            result.Images = releaseImages.Where(x => x.Bytes != null)
                                         .GroupBy(x => x.Signature)
                                         .Select(x => x.First()).Take(Configuration.Processing.MaximumReleaseImagesToAdd)
                                         .ToList();

            if (releaseLabels.Any())
            {
                var sw2 = Stopwatch.StartNew();
                result.Labels = releaseLabels.GroupBy(x => x.CatalogNumber).Select(x => x.First()).Select(x =>
                    new ReleaseLabel
                    {
                        CatalogNumber = x.CatalogNumber,
                        BeginDate = x.BeginDate,
                        EndDate = x.EndDate,
                        Status = Statuses.New,
                        Label = new Label
                        {
                            Name = x.Label.LabelName,
                            SortName = x.Label.LabelSortName,
                            MusicBrainzId = x.Label.MusicBrainzId,
                            BeginDate = x.Label.StartDate,
                            EndDate = x.Label.EndDate,
                            ImageUrl = x.Label.LabelImageUrl,
                            AlternateNames = x.Label.AlternateNames.ToDelimitedList(),
                            URLs = x.Label.Urls.ToDelimitedList(),
                            Status = Statuses.New
                        }
                    }).ToList();
                sw2.Stop();
                Logger.LogTrace($"PerformMetaDataProvidersReleaseSearch: Release Labels Processing Complete [{ sw2.ElapsedMilliseconds }]");
            }
            if (releaseMedias.Any())
            {
                var sw2 = Stopwatch.StartNew();
                var resultReleaseMedias = new List<ReleaseMedia>();
                foreach (var releaseMedia in releaseMedias.GroupBy(x => x.ReleaseMediaNumber).Select(x => x.First()))
                {
                    var rm = new ReleaseMedia
                    {
                        MediaNumber = releaseMedia.ReleaseMediaNumber ?? 0,
                        SubTitle = releaseMedia.ReleaseMediaSubTitle,
                        TrackCount = releaseMedia.TrackCount ?? 0,
                        Status = Statuses.New
                    };
                    var rmTracks = new List<Track>();
                    foreach (var releaseTrack in releaseMedias
                        .Where(x => x.ReleaseMediaNumber == releaseMedia.ReleaseMediaNumber)
                        .SelectMany(x => x.Tracks)
                        .Where(x => x.TrackNumber.HasValue)
                        .OrderBy(x => x.TrackNumber))
                    {
                        var foundTrack = true;
                        var rmTrack = rmTracks.FirstOrDefault(x => x.TrackNumber == releaseTrack.TrackNumber.Value);
                        if (rmTrack == null)
                        {
                            Artist trackArtist = null;
                            if (releaseTrack.Artist != null)
                            {
                                trackArtist = new Artist
                                {
                                    Name = releaseTrack.Artist.ArtistName,
                                    SpotifyId = releaseTrack.Artist.SpotifyId,
                                    ArtistType = releaseTrack.Artist.ArtistType
                                };
                            }
                            rmTrack = new Track
                            {
                                TrackArtist = trackArtist,
                                TrackArtists = releaseTrack.Artists,
                                TrackNumber = releaseTrack.TrackNumber.Value,
                                MusicBrainzId = releaseTrack.MusicBrainzId,
                                SpotifyId = releaseTrack.SpotifyId,
                                AmgId = releaseTrack.AmgId,
                                Title = releaseTrack.Title,
                                AlternateNames = releaseTrack.AlternateNames.ToDelimitedList(),
                                Duration = releaseTrack.Duration,
                                Tags = releaseTrack.Tags.ToDelimitedList(),
                                ISRC = releaseTrack.ISRC,
                                LastFMId = releaseTrack.LastFMId,
                                Status = Statuses.New
                            };
                            foundTrack = false;
                        }

                        rmTrack.Duration = rmTrack.Duration ?? releaseTrack.Duration;
                        rmTrack.MusicBrainzId = rmTrack.MusicBrainzId ?? releaseTrack.MusicBrainzId;
                        rmTrack.SpotifyId = rmTrack.SpotifyId ?? releaseTrack.SpotifyId;
                        rmTrack.AmgId = rmTrack.AmgId ?? releaseTrack.AmgId;
                        rmTrack.Title = rmTrack.Title ?? releaseTrack.Title;
                        rmTrack.Duration = releaseTrack.Duration;
                        rmTrack.Tags = rmTrack.Tags == null
                            ? releaseTrack.Tags.ToDelimitedList()
                            : rmTrack.Tags.AddToDelimitedList(releaseTrack.Tags);
                        rmTrack.AlternateNames = rmTrack.AlternateNames == null
                            ? releaseTrack.AlternateNames.ToDelimitedList()
                            : rmTrack.AlternateNames.AddToDelimitedList(releaseTrack.AlternateNames);
                        rmTrack.ISRC = rmTrack.ISRC ?? releaseTrack.ISRC;
                        rmTrack.LastFMId = rmTrack.LastFMId ?? releaseTrack.LastFMId;
                        if (!foundTrack) rmTracks.Add(rmTrack);
                    }

                    rm.Tracks = rmTracks;
                    rm.TrackCount = (short)rmTracks.Count();
                    resultReleaseMedias.Add(rm);
                }

                result.Medias = resultReleaseMedias;
                result.TrackCount = (short)releaseMedias.SelectMany(x => x.Tracks).Count();
                sw2.Stop();
                Logger.LogTrace($"PerformMetaDataProvidersReleaseSearch: Release Media Processing Complete [{ sw2.ElapsedMilliseconds }]");
            }

            if (metaData.Images != null && metaData.Images.Any())
            {
                var image = metaData.Images.FirstOrDefault(x => x.Type == AudioMetaDataImageType.FrontCover);
                if (image == null) image = metaData.Images.FirstOrDefault();
            }

            if (!string.IsNullOrEmpty(artistFolder))
            {
                // If any file exist for cover that over-rides whatever if found in metadata providers.
                var releaseFolder = new DirectoryInfo(result.ReleaseFileFolder(artistFolder));
                if (releaseFolder.Exists)
                {
                    string coverFileName = null;
                    var cover = ImageHelper.FindImageTypeInDirectory(releaseFolder, ImageType.Release);
                    if (!cover.Any())
                    {
                        // See if cover exist by filename
                        var imageFilesInFolder =
                            ImageHelper.ImageFilesInFolder(releaseFolder.FullName, SearchOption.AllDirectories);
                        if (imageFilesInFolder != null && imageFilesInFolder.Any())
                        {
                            var imageCoverByReleaseName = imageFilesInFolder.FirstOrDefault(x =>
                                x == result.Title || x == result.Title.ToFileNameFriendly());
                            if (imageCoverByReleaseName != null) coverFileName = imageCoverByReleaseName;
                        }
                    }
                    else if (cover.Any())
                    {
                        coverFileName = cover.First().FullName;
                    }
                }
            }
            sw.Stop();
            return new OperationResult<Release>
            {
                Data = result,
                IsSuccess = result != null,
                Errors = resultsExceptions,
                OperationTime = sw.ElapsedMilliseconds
            };
        }
    }
}