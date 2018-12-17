using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Factories;
using Roadie.Library.Imaging;
using Roadie.Library.MetaData.Audio;
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
        private IArtistLookupEngine ArtistLookupEngine { get; }
        private ILabelLookupEngine LabelLookupEngine { get; }

        public List<int> _addedReleaseIds = new List<int>();
        public List<int> _addedTrackIds = new List<int>();

        public IEnumerable<int> AddedReleaseIds
        {
            get
            {
                return this._addedReleaseIds;
            }
        }

        public IEnumerable<int> AddedTrackIds
        {
            get
            {
                return this._addedTrackIds;
            }
        }
       

        public IReleaseSearchEngine MusicBrainzReleaseSearchEngine { get; }
        public IReleaseSearchEngine DiscogsReleaseSearchEngine { get; }
        public IReleaseSearchEngine ITunesReleaseSearchEngine { get; }
        public IReleaseSearchEngine LastFmReleaseSearchEngine { get; }
        public IReleaseSearchEngine SpotifyReleaseSearchEngine { get; }
        public IReleaseSearchEngine WikipediaReleaseSearchEngine { get; }

        public ReleaseLookupEngine(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context, ICacheManager cacheManager, ILogger logger, IArtistLookupEngine aristLookupEngine, ILabelLookupEngine labelLookupEngine)
            : base(configuration, httpEncoder, context, cacheManager, logger)
        {
            this.ArtistLookupEngine = ArtistLookupEngine;
            this.LabelLookupEngine = labelLookupEngine;

            this.ITunesReleaseSearchEngine = new ITunesSearchEngine(this.Configuration, this.CacheManager, this.Logger);
            this.MusicBrainzReleaseSearchEngine = new musicbrainz.MusicBrainzProvider(this.Configuration, this.CacheManager, this.Logger);
            this.LastFmReleaseSearchEngine = new lastfm.LastFmHelper(this.Configuration, this.CacheManager, this.Logger);
            this.DiscogsReleaseSearchEngine = new discogs.DiscogsHelper(this.Configuration, this.CacheManager, this.Logger);
            this.SpotifyReleaseSearchEngine = new spotify.SpotifyHelper(this.Configuration, this.CacheManager, this.Logger);
            this.WikipediaReleaseSearchEngine = new wikipedia.WikipediaHelper(this.Configuration, this.CacheManager, this.Logger, this.HttpEncoder);
        }

        public async Task<OperationResult<Data.Release>> GetByName(Data.Artist artist, AudioMetaData metaData, bool doFindIfNotInDatabase = false, bool doAddTracksInDatabase = false, int? submissionId = null)
        {
            SimpleContract.Requires<ArgumentNullException>(artist != null, "Invalid Artist");
            SimpleContract.Requires<ArgumentOutOfRangeException>(artist.Id > 0, "Invalid Artist Id");
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                var cacheRegion = (new Data.Release { Artist = artist, Title = metaData.Release }).CacheRegion;
                var cacheKey = string.Format("urn:release_by_artist_id_and_name:{0}:{1}", artist.Id, metaData.Release);
                var resultInCache = this.CacheManager.Get<Data.Release>(cacheKey, cacheRegion);
                if (resultInCache != null)
                {
                    sw.Stop();
                    return new OperationResult<Data.Release>
                    {
                        IsSuccess = true,
                        OperationTime = sw.ElapsedMilliseconds,
                        Data = resultInCache
                    };
                }
                var getParams = new List<object>();
                var searchName = metaData.Release.NormalizeName().ToLower();
                var specialSearchName = metaData.Release.ToAlphanumericName();
                getParams.Add(new MySqlParameter("@artistId", artist.Id));
                getParams.Add(new MySqlParameter("@isTitle", searchName));
                getParams.Add(new MySqlParameter("@startAlt", string.Format("{0}|%", searchName)));
                getParams.Add(new MySqlParameter("@inAlt", string.Format("%|{0}|%", searchName)));
                getParams.Add(new MySqlParameter("@endAlt", string.Format("%|{0}", searchName)));
                getParams.Add(new MySqlParameter("@sstartAlt", string.Format("{0}|%", specialSearchName)));
                getParams.Add(new MySqlParameter("@sinAlt", string.Format("%|{0}|%", specialSearchName)));
                getParams.Add(new MySqlParameter("@sendAlt", string.Format("%|{0}", specialSearchName)));
                var release = this.DbContext.Releases.FromSql(@"SELECT *
                FROM `release`
                WHERE artistId = @artistId
                AND (LCASE(title) = @isTitle
                OR LCASE(alternatenames) = @isTitle
                OR alternatenames like @startAlt
                OR alternatenames like @sstartAlt
                OR alternatenames like @inAlt
                OR alternatenames like @sinAlt
                OR alternatenames like @endAlt
                OR alternatenames like @sendAlt)
                LIMIT 1", getParams.ToArray()).FirstOrDefault();
                sw.Stop();
                if (release == null || !release.IsValid)
                {
                    this.Logger.LogInformation("ReleaseFactory: Release Not Found For Artist `{0}` MetaData [{1}]", artist.ToString(), metaData.ToString());
                    if (doFindIfNotInDatabase)
                    {
                        OperationResult<Data.Release> releaseSearch = new OperationResult<Data.Release>();
                        try
                        {
                            releaseSearch = await this.PerformMetaDataProvidersReleaseSearch(metaData, artist.ArtistFileFolder(this.Configuration, this.Configuration.LibraryFolder), submissionId);
                        }
                        catch (Exception ex)
                        {
                            sw.Stop();
                            this.Logger.LogError(ex);
                            return new OperationResult<Data.Release>
                            {
                                OperationTime = sw.ElapsedMilliseconds,
                                Errors = new Exception[1] { ex }
                            };
                        }
                        if (releaseSearch.IsSuccess)
                        {
                            release = releaseSearch.Data;
                            release.ArtistId = artist.Id;
                            var addResult = await this.Add(release, doAddTracksInDatabase);
                            if (!addResult.IsSuccess)
                            {
                                sw.Stop();
                                return new OperationResult<Data.Release>
                                {
                                    OperationTime = sw.ElapsedMilliseconds,
                                    Errors = addResult.Errors
                                };
                            }
                        }
                    }
                }
                if (release != null)
                {
                    this.CacheManager.Add(cacheKey, release);
                }
                return new OperationResult<Data.Release>
                {
                    IsSuccess = release != null,
                    OperationTime = sw.ElapsedMilliseconds,
                    Data = release
                };
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
            }
            return new OperationResult<Data.Release>();
        }

        public async Task<OperationResult<Data.Release>> Add(Data.Release release, bool doAddTracksInDatabase = false)
        {
            SimpleContract.Requires<ArgumentNullException>(release != null, "Invalid Release");

            try
            {
                var releaseGenreTables = release.Genres;
                var releaseImages = release.Images;
                var releaseMedias = release.Medias;
                var releaseLabels = release.Labels;
                var now = DateTime.UtcNow;
                release.AlternateNames = release.AlternateNames.AddToDelimitedList(new string[] { release.Title.ToAlphanumericName() });
                release.Images = null;
                release.Labels = null;
                release.Medias = null;
                release.Genres = null;
                release.LibraryStatus = LibraryStatus.Incomplete;
                release.Status = Statuses.New;
                if (!release.IsValid)
                {
                    return new OperationResult<Data.Release>
                    {
                        Errors = new Exception[1] { new Exception("Release is Invalid") }
                    };
                }
                this.DbContext.Releases.Add(release);
                int inserted = 0;
                try
                {
                    inserted = await this.DbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, ex.Serialize());
                }
                if (inserted > 0 && release.Id > 0)
                {
                    this._addedReleaseIds.Add(release.Id);
                    if (releaseGenreTables != null && releaseGenreTables.Any(x => x.GenreId == null))
                    {
                        foreach (var releaseGenreTable in releaseGenreTables)
                        {
                            var genreName = releaseGenreTable.Genre.Name.ToLower().Trim();
                            if (string.IsNullOrEmpty(genreName))
                            {
                                continue;
                            }
                            var genre = this.DbContext.Genres.FirstOrDefault(x => x.Name.ToLower().Trim() == genreName);
                            if (genre == null)
                            {
                                genre = new Genre
                                {
                                    Name = releaseGenreTable.Genre.Name
                                };
                                this.DbContext.Genres.Add(genre);
                                await this.DbContext.SaveChangesAsync();
                            }
                            if (genre != null && genre.Id > 0)
                            {
                                string sql = null;
                                try
                                {
                                    sql = string.Format("INSERT INTO `releaseGenreTable` (releaseId, genreId) VALUES ({0}, {1});", release.Id, genre.Id);
                                    await this.DbContext.Database.ExecuteSqlCommandAsync(sql);
                                }
                                catch (Exception ex)
                                {
                                    this.Logger.LogError(ex, "Sql [" + sql + "]");
                                }
                            }
                        }
                    }
                    if (releaseImages != null && releaseImages.Any(x => x.Status == Statuses.New))
                    {
                        foreach (var releaseImage in releaseImages)
                        {
                            this.DbContext.Images.Add(new Data.Image
                            {
                                ReleaseId = release.Id,
                                Url = releaseImage.Url,
                                Signature = releaseImage.Signature,
                                Bytes = releaseImage.Bytes
                            });
                        }
                        try
                        {
                            await this.DbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogError(ex);
                        }
                    }

                    if (releaseLabels != null && releaseLabels.Any(x => x.Status == Statuses.New))
                    {
                        foreach (var neweleaseLabel in releaseLabels.Where(x => x.Status == Statuses.New))
                        {
                            var labelFetch = await this.LabelLookupEngine.GetByName(neweleaseLabel.Label.Name, true);
                            if (labelFetch.IsSuccess)
                            {
                                this.DbContext.ReleaseLabels.Add(new Data.ReleaseLabel
                                {
                                    CatalogNumber = neweleaseLabel.CatalogNumber,
                                    BeginDate = neweleaseLabel.BeginDate,
                                    EndDate = neweleaseLabel.EndDate,
                                    ReleaseId = release.Id,
                                    LabelId = labelFetch.Data.Id
                                });
                            }
                        }
                        try
                        {
                            await this.DbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogError(ex);
                        }
                    }
                    if (doAddTracksInDatabase)
                    {
                        if (releaseMedias != null && releaseMedias.Any(x => x.Status == Statuses.New))
                        {
                            foreach (var newReleaseMedia in releaseMedias.Where(x => x.Status == Statuses.New))
                            {
                                var releasemedia = new Data.ReleaseMedia
                                {
                                    Status = Statuses.Incomplete,
                                    MediaNumber = newReleaseMedia.MediaNumber,
                                    SubTitle = newReleaseMedia.SubTitle,
                                    TrackCount = newReleaseMedia.TrackCount,
                                    ReleaseId = release.Id
                                };
                                var releasemediatracks = new List<Data.Track>();
                                foreach (var newTrack in newReleaseMedia.Tracks)
                                {
                                    int? trackArtistId = null;
                                    string partTitles = null;
                                    if (newTrack.TrackArtist != null)
                                    {
                                        if (!release.IsCastRecording)
                                        {
                                            var trackArtistData = await this.ArtistLookupEngine.GetByName(new AudioMetaData { Artist = newTrack.TrackArtist.Name }, true);
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
                                    releasemediatracks.Add(new Data.Track
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
                                this.DbContext.ReleaseMedias.Add(releasemedia);
                                this._addedTrackIds.AddRange(releasemedia.Tracks.Select(x => x.Id));
                            }
                            try
                            {
                                await this.DbContext.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                this.Logger.LogError(ex);
                            }
                        }
                    }

                    this.Logger.LogInformation("Added New Release: `{0}`", release.ToString());
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Serialize());
            }
            return new OperationResult<Data.Release>
            {
                IsSuccess = release.Id > 0,
                Data = release
            };
        }

        public async Task<OperationResult<Data.Release>> PerformMetaDataProvidersReleaseSearch(AudioMetaData metaData, string artistFolder = null, int? submissionId = null)
        {
            SimpleContract.Requires<ArgumentNullException>(metaData != null, "Invalid MetaData");

            var sw = new Stopwatch();
            sw.Start();

            var result = new Data.Release
            {
                Title = metaData.Release.ToTitleCase(false),
                TrackCount = (short)(metaData.TotalTrackNumbers ?? 0),
                ReleaseDate = SafeParser.ToDateTime(metaData.Year),
                SubmissionId = submissionId
            };
            var resultsExceptions = new List<Exception>();
            var releaseGenres = new List<string>();
            // Add any Genre found in the given MetaData
            if (metaData.Genres != null)
            {
                releaseGenres.AddRange(metaData.Genres);
            }
            var releaseLabels = new List<ReleaseLabelSearchResult>();
            var releaseMedias = new List<ReleaseMediaSearchResult>();
            var releaseImageUrls = new List<string>();

            var dontDoMetaDataProvidersSearchArtists = this.Configuration.DontDoMetaDataProvidersSearchArtists;
            if (!dontDoMetaDataProvidersSearchArtists.Any(x => x.Equals(metaData.Artist, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    #region ITunes

                    if (this.ITunesReleaseSearchEngine.IsEnabled)
                    {
                        this.Logger.LogTrace("ITunesReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var iTunesResult = await this.ITunesReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (iTunesResult.IsSuccess)
                        {
                            var i = iTunesResult.Data.First();
                            if (i.AlternateNames != null)
                            {
                                result.AlternateNames = result.AlternateNames.AddToDelimitedList(i.AlternateNames);
                            }
                            if (i.Tags != null)
                            {
                                result.Tags = result.Tags.AddToDelimitedList(i.Tags);
                            }
                            if (i.Urls != null)
                            {
                                result.URLs = result.URLs.AddToDelimitedList(i.Urls);
                            }
                            if (i.ImageUrls != null)
                            {
                                releaseImageUrls.AddRange(i.ImageUrls);
                            }
                            if (i.ReleaseGenres != null)
                            {
                                releaseGenres.AddRange(i.ReleaseGenres);
                            }
                            result.CopyTo(new Data.Release
                            {
                                ReleaseDate = result.ReleaseDate ?? i.ReleaseDate,
                                AmgId = i.AmgId,
                                Profile = i.Profile,
                                ITunesId = i.iTunesId,
                                Title = result.Title ?? i.ReleaseTitle,
                                Thumbnail = i.ReleaseThumbnailUrl != null ? WebHelper.BytesForImageUrl(i.ReleaseThumbnailUrl) : null,
                                ReleaseType = result.ReleaseType == ReleaseType.Unknown ? SafeParser.ToEnum<ReleaseType>(i.ReleaseType) : result.ReleaseType
                            });
                            if (i.ReleaseLabel != null)
                            {
                                releaseLabels.AddRange(i.ReleaseLabel);
                            }
                            if (i.ReleaseMedia != null)
                            {
                                releaseMedias.AddRange(i.ReleaseMedia);
                            }
                        }
                        if (iTunesResult.Errors != null)
                        {
                            resultsExceptions.AddRange(iTunesResult.Errors);
                        }
                    }

                    #endregion ITunes

                    #region MusicBrainz

                    if (this.MusicBrainzReleaseSearchEngine.IsEnabled)
                    {
                        this.Logger.LogTrace("MusicBrainzReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var mbResult = await this.MusicBrainzReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (mbResult.IsSuccess)
                        {
                            var mb = mbResult.Data.First();
                            if (mb.AlternateNames != null)
                            {
                                result.AlternateNames = result.AlternateNames.AddToDelimitedList(mb.AlternateNames);
                            }
                            if (mb.Tags != null)
                            {
                                result.Tags = result.Tags.AddToDelimitedList(mb.Tags);
                            }
                            if (mb.Urls != null)
                            {
                                result.URLs = result.URLs.AddToDelimitedList(mb.Urls);
                            }
                            if (mb.ImageUrls != null)
                            {
                                releaseImageUrls.AddRange(mb.ImageUrls);
                            }
                            if (mb.ReleaseGenres != null)
                            {
                                releaseGenres.AddRange(mb.ReleaseGenres);
                            }
                            if (!string.IsNullOrEmpty(mb.ReleaseTitle) && !mb.ReleaseTitle.Equals(result.Title, StringComparison.OrdinalIgnoreCase))
                            {
                                result.AlternateNames.AddToDelimitedList(new string[] { mb.ReleaseTitle });
                            }
                            result.CopyTo(new Data.Release
                            {
                                ReleaseDate = result.ReleaseDate ?? mb.ReleaseDate,
                                AmgId = mb.AmgId,
                                Profile = mb.Profile,
                                MusicBrainzId = mb.MusicBrainzId,
                                ITunesId = mb.iTunesId,
                                Title = result.Title ?? mb.ReleaseTitle,
                                Thumbnail = mb.ReleaseThumbnailUrl != null ? WebHelper.BytesForImageUrl(mb.ReleaseThumbnailUrl) : null,
                                ReleaseType = result.ReleaseType == ReleaseType.Unknown ? SafeParser.ToEnum<ReleaseType>(mb.ReleaseType) : result.ReleaseType
                            });
                            if (mb.ReleaseLabel != null)
                            {
                                releaseLabels.AddRange(mb.ReleaseLabel);
                            }
                            if (mb.ReleaseMedia != null)
                            {
                                releaseMedias.AddRange(mb.ReleaseMedia);
                            }
                        }
                        if (mbResult.Errors != null)
                        {
                            resultsExceptions.AddRange(mbResult.Errors);
                        }
                    }

                    #endregion MusicBrainz

                    #region LastFm

                    if (this.LastFmReleaseSearchEngine.IsEnabled)
                    {
                        this.Logger.LogTrace("LastFmReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var lastFmResult = await this.LastFmReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (lastFmResult.IsSuccess)
                        {
                            var l = lastFmResult.Data.First();
                            if (l.AlternateNames != null)
                            {
                                result.AlternateNames = result.AlternateNames.AddToDelimitedList(l.AlternateNames);
                            }
                            if (l.Tags != null)
                            {
                                result.Tags = result.Tags.AddToDelimitedList(l.Tags);
                            }
                            if (l.Urls != null)
                            {
                                result.URLs = result.URLs.AddToDelimitedList(l.Urls);
                            }
                            if (l.ImageUrls != null)
                            {
                                releaseImageUrls.AddRange(l.ImageUrls);
                            }
                            if (l.ReleaseGenres != null)
                            {
                                releaseGenres.AddRange(l.ReleaseGenres);
                            }
                            if (!string.IsNullOrEmpty(l.ReleaseTitle) && !l.ReleaseTitle.Equals(result.Title, StringComparison.OrdinalIgnoreCase))
                            {
                                result.AlternateNames.AddToDelimitedList(new string[] { l.ReleaseTitle });
                            }
                            result.CopyTo(new Data.Release
                            {
                                ReleaseDate = result.ReleaseDate ?? l.ReleaseDate,
                                AmgId = l.AmgId,
                                Profile = l.Profile,
                                LastFMId = l.LastFMId,
                                LastFMSummary = l.LastFMSummary,
                                MusicBrainzId = l.MusicBrainzId,
                                ITunesId = l.iTunesId,
                                Title = result.Title ?? l.ReleaseTitle,
                                Thumbnail = l.ReleaseThumbnailUrl != null ? WebHelper.BytesForImageUrl(l.ReleaseThumbnailUrl) : null,
                                ReleaseType = result.ReleaseType == ReleaseType.Unknown ? SafeParser.ToEnum<ReleaseType>(l.ReleaseType) : result.ReleaseType
                            });
                            if (l.ReleaseLabel != null)
                            {
                                releaseLabels.AddRange(l.ReleaseLabel);
                            }
                            if (l.ReleaseMedia != null)
                            {
                                releaseMedias.AddRange(l.ReleaseMedia);
                            }
                        }
                        if (lastFmResult.Errors != null)
                        {
                            resultsExceptions.AddRange(lastFmResult.Errors);
                        }
                    }

                    #endregion LastFm

                    #region Spotify

                    if (this.SpotifyReleaseSearchEngine.IsEnabled)
                    {
                        this.Logger.LogTrace("SpotifyReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var spotifyResult = await this.SpotifyReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (spotifyResult.IsSuccess)
                        {
                            var s = spotifyResult.Data.First();
                            if (s.Tags != null)
                            {
                                result.Tags = result.Tags.AddToDelimitedList(s.Tags);
                            }
                            if (s.Urls != null)
                            {
                                result.URLs = result.URLs.AddToDelimitedList(s.Urls);
                            }
                            if (s.ImageUrls != null)
                            {
                                releaseImageUrls.AddRange(s.ImageUrls);
                            }
                            if (s.ReleaseGenres != null)
                            {
                                releaseGenres.AddRange(s.ReleaseGenres);
                            }
                            if (!string.IsNullOrEmpty(s.ReleaseTitle) && !s.ReleaseTitle.Equals(result.Title, StringComparison.OrdinalIgnoreCase))
                            {
                                result.AlternateNames.AddToDelimitedList(new string[] { s.ReleaseTitle });
                            }
                            result.CopyTo(new Data.Release
                            {
                                ReleaseDate = result.ReleaseDate ?? s.ReleaseDate,
                                AmgId = s.AmgId,
                                Profile = this.HttpEncoder.HtmlEncode(s.Profile),
                                SpotifyId = s.SpotifyId,
                                MusicBrainzId = s.MusicBrainzId,
                                ITunesId = s.iTunesId,
                                Title = result.Title ?? s.ReleaseTitle,
                                Thumbnail = s.ReleaseThumbnailUrl != null ? WebHelper.BytesForImageUrl(s.ReleaseThumbnailUrl) : null,
                                ReleaseType = result.ReleaseType == ReleaseType.Unknown ? SafeParser.ToEnum<ReleaseType>(s.ReleaseType) : result.ReleaseType
                            });
                            if (s.ReleaseLabel != null)
                            {
                                releaseLabels.AddRange(s.ReleaseLabel);
                            }
                            if (s.ReleaseMedia != null)
                            {
                                releaseMedias.AddRange(s.ReleaseMedia);
                            }
                        }
                        if (spotifyResult.Errors != null)
                        {
                            resultsExceptions.AddRange(spotifyResult.Errors);
                        }
                    }

                    #endregion Spotify

                    #region Discogs

                    if (this.DiscogsReleaseSearchEngine.IsEnabled)
                    {
                        this.Logger.LogTrace("DiscogsReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var discogsResult = await this.DiscogsReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (discogsResult.IsSuccess)
                        {
                            var d = discogsResult.Data.First();
                            if (d.Urls != null)
                            {
                                result.URLs = result.URLs.AddToDelimitedList(d.Urls);
                            }
                            if (d.ImageUrls != null)
                            {
                                releaseImageUrls.AddRange(d.ImageUrls);
                            }
                            if (d.AlternateNames != null)
                            {
                                result.AlternateNames = result.AlternateNames.AddToDelimitedList(d.AlternateNames);
                            }
                            if (!string.IsNullOrEmpty(d.ReleaseTitle) && !d.ReleaseTitle.Equals(result.Title, StringComparison.OrdinalIgnoreCase))
                            {
                                result.AlternateNames.AddToDelimitedList(new string[] { d.ReleaseTitle });
                            }
                            result.CopyTo(new Data.Release
                            {
                                Profile = this.HttpEncoder.HtmlEncode(d.Profile),
                                DiscogsId = d.DiscogsId,
                                Title = result.Title ?? d.ReleaseTitle,
                                Thumbnail = d.ReleaseThumbnailUrl != null ? WebHelper.BytesForImageUrl(d.ReleaseThumbnailUrl) : null,
                                ReleaseType = result.ReleaseType == ReleaseType.Unknown ? SafeParser.ToEnum<ReleaseType>(d.ReleaseType) : result.ReleaseType
                            });
                            if (d.ReleaseLabel != null)
                            {
                                releaseLabels.AddRange(d.ReleaseLabel);
                            }
                            if (d.ReleaseMedia != null)
                            {
                                releaseMedias.AddRange(d.ReleaseMedia);
                            }
                        }
                        if (discogsResult.Errors != null)
                        {
                            resultsExceptions.AddRange(discogsResult.Errors);
                        }
                    }

                    #endregion Discogs
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex);
                }

                this.Logger.LogTrace("Metadata Providers Search Complete. [{0}]", sw.ElapsedMilliseconds);
            }
            else
            {
                this.Logger.LogTrace("Skipped Metadata Providers Search, DontDoMetaDataProvidersSearchArtists set for Artist [{0}].", metaData.Artist);
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
                result.Genres = new List<ReleaseGenre>();
                foreach (var releaseGenre in releaseGenres.Where(x => !string.IsNullOrEmpty(x)).GroupBy(x => x).Select(x => x.First()))
                {
                    var rg = releaseGenre.Trim();
                    if (!string.IsNullOrEmpty(rg))
                    {
                        result.Genres.Add(new Data.ReleaseGenre
                        {
                            Genre = (this.DbContext.Genres.Where(x => x.Name.ToLower() == rg.ToLower()).FirstOrDefault() ?? new Data.Genre { Name = rg })
                        });
                    }
                };
            }
            if (releaseImageUrls.Any())
            {
                var imageBag = new ConcurrentBag<Data.Image>();
                var i = releaseImageUrls.Select(async url =>
                {
                    imageBag.Add(await WebHelper.GetImageFromUrlAsync(url));
                });
                await Task.WhenAll(i);
                // If the release has images merge any fetched images
                var existingImages = result.Images != null ? result.Images.ToList() : new List<Data.Image>();
                existingImages.AddRange(imageBag.ToList());
                // Now set release images to be unique image based on image hash
                result.Images = existingImages.Where(x => x != null && x.Bytes != null).GroupBy(x => x.Signature).Select(x => x.First()).Take(this.Configuration.Processing.MaximumReleaseImagesToAdd).ToList();
                if (result.Thumbnail == null && result.Images != null)
                {
                    result.Thumbnail = result.Images.First().Bytes;
                }
            }

            if (releaseLabels.Any())
            {
                result.Labels = releaseLabels.GroupBy(x => x.CatalogNumber).Select(x => x.First()).Select(x => new Data.ReleaseLabel
                {
                    CatalogNumber = x.CatalogNumber,
                    BeginDate = x.BeginDate,
                    EndDate = x.EndDate,
                    Status = Statuses.New,
                    Label = new Data.Label
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
            }

            if (releaseMedias.Any())
            {
                var resultReleaseMedias = new List<Data.ReleaseMedia>();
                foreach (var releaseMedia in releaseMedias.GroupBy(x => x.ReleaseMediaNumber).Select(x => x.First()))
                {
                    var rm = new Data.ReleaseMedia
                    {
                        MediaNumber = releaseMedia.ReleaseMediaNumber ?? 0,
                        SubTitle = releaseMedia.ReleaseMediaSubTitle,
                        TrackCount = releaseMedia.TrackCount ?? 0,
                        Status = Statuses.New
                    };
                    var rmTracks = new List<Data.Track>();
                    foreach (var releaseTrack in releaseMedias.Where(x => x.ReleaseMediaNumber == releaseMedia.ReleaseMediaNumber)
                                                             .SelectMany(x => x.Tracks)
                                                             .Where(x => x.TrackNumber.HasValue)
                                                             .OrderBy(x => x.TrackNumber))
                    {
                        var foundTrack = true;
                        var rmTrack = rmTracks.FirstOrDefault(x => x.TrackNumber == releaseTrack.TrackNumber.Value);
                        if (rmTrack == null)
                        {
                            Data.Artist trackArtist = null;
                            if (releaseTrack.Artist != null)
                            {
                                trackArtist = new Data.Artist
                                {
                                    Name = releaseTrack.Artist.ArtistName,
                                    SpotifyId = releaseTrack.Artist.SpotifyId,
                                    ArtistType = releaseTrack.Artist.ArtistType
                                };
                            }
                            rmTrack = new Data.Track
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
                        rmTrack.Tags = rmTrack.Tags == null ? releaseTrack.Tags.ToDelimitedList() : rmTrack.Tags.AddToDelimitedList(releaseTrack.Tags);
                        rmTrack.AlternateNames = rmTrack.AlternateNames == null ? releaseTrack.AlternateNames.ToDelimitedList() : rmTrack.AlternateNames.AddToDelimitedList(releaseTrack.AlternateNames);
                        rmTrack.ISRC = rmTrack.ISRC ?? releaseTrack.ISRC;
                        rmTrack.LastFMId = rmTrack.LastFMId ?? releaseTrack.LastFMId;
                        if (!foundTrack)
                        {
                            rmTracks.Add(rmTrack);
                        }
                    }
                    rm.Tracks = rmTracks;
                    rm.TrackCount = (short)rmTracks.Count();
                    resultReleaseMedias.Add(rm);
                }
                result.Medias = resultReleaseMedias;
                result.TrackCount = (short)releaseMedias.SelectMany(x => x.Tracks).Count();
            }

            if (metaData.Images != null && metaData.Images.Any())
            {
                var image = metaData.Images.FirstOrDefault(x => x.Type == AudioMetaDataImageType.FrontCover);
                if (image == null)
                {
                    image = metaData.Images.FirstOrDefault();
                }
                // If there is an image on the metadata file itself then that over-rides metadata providers.
                if (image != null)
                {
                    result.Thumbnail = image.Data;
                }
            }
            if (!string.IsNullOrEmpty(artistFolder))
            {
                // If any file exist for cover that over-rides whatever if found in metadata providers.
                var releaseFolder = result.ReleaseFileFolder(artistFolder);
                if (Directory.Exists(releaseFolder))
                {
                    // See if there is a cover file ("cover.jpg") if so set thumbnail image to that
                    var coverFileName = Path.Combine(releaseFolder, ReleaseFactory.CoverFilename);
                    if(!File.Exists(coverFileName))
                    {
                        // See if any file exists in the release folder with "cover" in the name
                        var coverFiles = Directory.GetFiles(releaseFolder, "*cover*.jpg", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });
                        if(coverFiles != null && coverFiles.Any())
                        {
                            coverFileName = coverFiles.First();
                        }
                    }
                    if (File.Exists(coverFileName))
                    {
                        // Read image and convert to jpeg
                        result.Thumbnail = File.ReadAllBytes(coverFileName);
                        this.Logger.LogDebug("Using Release Cover File [{0}]", coverFileName);
                    }
                }
            }

            if (result.Thumbnail != null)
            {
                result.Thumbnail = ImageHelper.ResizeImage(result.Thumbnail, this.Configuration.ThumbnailImageSize.Width, this.Configuration.ThumbnailImageSize.Height);
                result.Thumbnail = ImageHelper.ConvertToJpegFormat(result.Thumbnail);
            }
            sw.Stop();
            return new OperationResult<Data.Release>
            {
                Data = result,
                IsSuccess = result != null,
                Errors = resultsExceptions,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

    }
}
