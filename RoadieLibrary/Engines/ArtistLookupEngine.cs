using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.SearchEngines.Imaging;
using Roadie.Library.SearchEngines.MetaData;
using Roadie.Library.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using discogs = Roadie.Library.SearchEngines.MetaData.Discogs;
using lastfm = Roadie.Library.MetaData.LastFm;
using musicbrainz = Roadie.Library.MetaData.MusicBrainz;
using spotify = Roadie.Library.SearchEngines.MetaData.Spotify;
using wikipedia = Roadie.Library.SearchEngines.MetaData.Wikipedia;

namespace Roadie.Library.Engines
{
    public class ArtistLookupEngine : LookupEngineBase, IArtistLookupEngine
    {
        public List<int> AddedArtistIds { get; } = new List<int>();
        public IArtistSearchEngine ITunesArtistSearchEngine { get; }
        public IArtistSearchEngine DiscogsArtistSearchEngine { get; }
        public IArtistSearchEngine LastFmArtistSearchEngine { get; }
        public IArtistSearchEngine MusicBrainzArtistSearchEngine { get; }
        public IArtistSearchEngine SpotifyArtistSearchEngine { get; }
        public IArtistSearchEngine WikipediaArtistSearchEngine { get; }


        public ArtistLookupEngine(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context, ICacheManager cacheManager, ILogger logger)
            : base(configuration, httpEncoder, context, cacheManager, logger)
        {

            this.ITunesArtistSearchEngine = new ITunesSearchEngine(this.Configuration, this.CacheManager, this.Logger);
            this.MusicBrainzArtistSearchEngine = new musicbrainz.MusicBrainzProvider(this.Configuration, this.CacheManager, this.Logger);
            this.LastFmArtistSearchEngine = new lastfm.LastFmHelper(this.Configuration, this.CacheManager, this.Logger);
            this.SpotifyArtistSearchEngine = new spotify.SpotifyHelper(this.Configuration, this.CacheManager, this.Logger);
            this.WikipediaArtistSearchEngine = new wikipedia.WikipediaHelper(this.Configuration, this.CacheManager, this.Logger, this.HttpEncoder);
            this.DiscogsArtistSearchEngine = new discogs.DiscogsHelper(this.Configuration, this.CacheManager, this.Logger);
        }

        public async Task<OperationResult<Artist>> Add(Artist artist)
        {
            SimpleContract.Requires<ArgumentNullException>(artist != null, "Invalid Artist");

            try
            {
                var ArtistGenreTables = artist.Genres;
                var ArtistImages = artist.Images;
                var now = DateTime.UtcNow;
                artist.AlternateNames = artist.AlternateNames.AddToDelimitedList(new string[] { artist.Name.ToAlphanumericName() });
                artist.Genres = null;
                artist.Images = null;
                if (artist.Thumbnail == null && ArtistImages != null)
                {
                    // Set the thumbnail to the first image
                    var firstImageWithNotNullBytes = ArtistImages.Where(x => x.Bytes != null).FirstOrDefault();
                    if (firstImageWithNotNullBytes != null)
                    {
                        artist.Thumbnail = firstImageWithNotNullBytes.Bytes;
                        if (artist.Thumbnail != null)
                        {
                            artist.Thumbnail = ImageHelper.ResizeImage(artist.Thumbnail, this.Configuration.ThumbnailImageSize.Width, this.Configuration.ThumbnailImageSize.Height);
                            artist.Thumbnail = ImageHelper.ConvertToJpegFormat(artist.Thumbnail);
                        }
                    }
                }
                if (!artist.IsValid)
                {
                    return new OperationResult<Artist>
                    {
                        Errors = new Exception[1] { new Exception("Artist is Invalid") }
                    };
                }
                var addArtistResult = this.DbContext.Artists.Add(artist);
                int inserted = 0;
                inserted = await this.DbContext.SaveChangesAsync();
                this.AddedArtistIds.Add(artist.Id);
                if (artist.Id < 1 && addArtistResult.Entity.Id > 0)
                {
                    artist.Id = addArtistResult.Entity.Id;
                }
                if (inserted > 0 && artist.Id > 0)
                {
                    if (ArtistGenreTables != null && ArtistGenreTables.Any(x => x.GenreId == null))
                    {
                        string sql = null;
                        try
                        {
                            foreach (var ArtistGenreTable in ArtistGenreTables)
                            {
                                var genre = this.DbContext.Genres.FirstOrDefault(x => x.Name.ToLower().Trim() == ArtistGenreTable.Genre.Name.ToLower().Trim());
                                if (genre == null)
                                {
                                    genre = new Genre
                                    {
                                        Name = ArtistGenreTable.Genre.Name
                                    };
                                    this.DbContext.Genres.Add(genre);
                                    await this.DbContext.SaveChangesAsync();
                                }
                                if (genre != null && genre.Id > 0)
                                {
                                    sql = string.Format("INSERT INTO `artistGenreTable` (artistId, genreId) VALUES ({0}, {1});", artist.Id, genre.Id);
                                    await this.DbContext.Database.ExecuteSqlCommandAsync(sql);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogError(ex, "Sql [" + sql + "] Exception [" + ex.Serialize() + "]");
                        }
                    }
                    if (ArtistImages != null && ArtistImages.Any(x => x.Status == Statuses.New))
                    {
                        foreach (var ArtistImage in ArtistImages)
                        {
                            this.DbContext.Images.Add(new Image
                            {
                                ArtistId = artist.Id,
                                Url = ArtistImage.Url,
                                Signature = ArtistImage.Signature,
                                Bytes = ArtistImage.Bytes
                            });
                        }
                        inserted = await this.DbContext.SaveChangesAsync();
                    }
                    this.Logger.LogInformation("Added New Artist: [{0}]", artist.ToString());
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Serialize());
            }
            return new OperationResult<Artist>
            {
                IsSuccess = artist.Id > 0,
                Data = artist
            };
        }

        public async Task<OperationResult<Artist>> GetByName(AudioMetaData metaData, bool doFindIfNotInDatabase = false)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                var ArtistName = metaData.Artist ?? metaData.TrackArtist;
                var cacheRegion = (new Artist { Name = ArtistName }).CacheRegion;
                var cacheKey = string.Format("urn:Artist_by_name:{0}", ArtistName);
                var resultInCache = this.CacheManager.Get<Artist>(cacheKey, cacheRegion);
                if (resultInCache != null)
                {
                    sw.Stop();
                    return new OperationResult<Artist>
                    {
                        IsSuccess = true,
                        OperationTime = sw.ElapsedMilliseconds,
                        Data = resultInCache
                    };
                }
                var artist = this.DatabaseQueryForArtistName(ArtistName);
                sw.Stop();
                if (artist == null || !artist.IsValid)
                {
                    this.Logger.LogInformation("ArtistFactory: Artist Not Found By Name [{0}]", ArtistName);
                    if (doFindIfNotInDatabase)
                    {
                        OperationResult<Artist> ArtistSearch = null;
                        try
                        {
                            ArtistSearch = await this.PerformMetaDataProvidersArtistSearch(metaData);
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogError(ex, ex.Serialize());
                        }
                        if (ArtistSearch.IsSuccess)
                        {
                            artist = ArtistSearch.Data;
                            // See if Artist already exist with either Name or Sort Name
                            var alreadyExists = this.DatabaseQueryForArtistName(ArtistSearch.Data.Name, ArtistSearch.Data.SortNameValue);
                            if (alreadyExists == null || !alreadyExists.IsValid)
                            {
                                var addResult = await this.Add(artist);
                                if (!addResult.IsSuccess)
                                {
                                    sw.Stop();
                                    this.Logger.LogWarning("Unable To Add Artist For MetaData [{0}]", metaData.ToString());
                                    return new OperationResult<Artist>
                                    {
                                        OperationTime = sw.ElapsedMilliseconds,
                                        Errors = addResult.Errors
                                    };
                                }
                                artist = addResult.Data;
                            }
                            else
                            {
                                artist = alreadyExists;
                            }
                        }
                    }
                }
                if (artist != null && artist.IsValid)
                {
                    this.CacheManager.Add(cacheKey, artist);
                }
                return new OperationResult<Artist>
                {
                    IsSuccess = artist != null,
                    OperationTime = sw.ElapsedMilliseconds,
                    Data = artist
                };
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Serialize());
            }
            return new OperationResult<Artist>();
        }

        public async Task<OperationResult<Artist>> PerformMetaDataProvidersArtistSearch(AudioMetaData metaData)
        {
            SimpleContract.Requires<ArgumentNullException>(metaData != null, "Invalid MetaData");
            SimpleContract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(metaData.Artist), "Invalid MetaData, Missing Artist");

            var sw = new Stopwatch();
            sw.Start();
            var result = new Artist
            {
                Name = metaData.Artist.ToTitleCase(false)
            };
            var resultsExceptions = new List<Exception>();
            var ArtistGenres = new List<string>();
            var ArtistImageUrls = new List<string>();
            var ArtistName = metaData.Artist;

            try
            {
                if (this.ITunesArtistSearchEngine.IsEnabled)
                {
                    var iTunesResult = await this.ITunesArtistSearchEngine.PerformArtistSearch(ArtistName, 1);
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
                        if (i.ISNIs != null)
                        {
                            result.ISNIList = result.ISNIList.AddToDelimitedList(i.ISNIs);
                        }
                        if (i.ImageUrls != null)
                        {
                            ArtistImageUrls.AddRange(i.ImageUrls);
                        }
                        if (i.ArtistGenres != null)
                        {
                            ArtistGenres.AddRange(i.ArtistGenres);
                        }
                        result.CopyTo(new Artist
                        {
                            EndDate = i.EndDate,
                            BioContext = i.Bio,
                            Profile = i.Profile,
                            ITunesId = i.iTunesId,
                            BeginDate = i.BeginDate,
                            Name = result.Name ?? i.ArtistName,
                            SortName = result.SortName ?? i.ArtistSortName,
                            Thumbnail = i.ArtistThumbnailUrl != null ? WebHelper.BytesForImageUrl(i.ArtistThumbnailUrl) : null,
                            ArtistType = result.ArtistType ?? i.ArtistType
                        });
                    }
                    if (iTunesResult.Errors != null)
                    {
                        resultsExceptions.AddRange(iTunesResult.Errors);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "iTunesArtistSearch: " + ex.Serialize());
            }
            try
            {
                if (this.MusicBrainzArtistSearchEngine.IsEnabled)
                {
                    var mbResult = await this.MusicBrainzArtistSearchEngine.PerformArtistSearch(result.Name, 1);
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
                        if (mb.ISNIs != null)
                        {
                            result.ISNIList = result.ISNIList.AddToDelimitedList(mb.ISNIs);
                        }
                        if (mb.ImageUrls != null)
                        {
                            ArtistImageUrls.AddRange(mb.ImageUrls);
                        }
                        if (mb.ArtistGenres != null)
                        {
                            ArtistGenres.AddRange(mb.ArtistGenres);
                        }
                        if (!string.IsNullOrEmpty(mb.ArtistName) && !mb.ArtistName.Equals(result.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            result.AlternateNames.AddToDelimitedList(new string[] { mb.ArtistName });
                        }
                        result.CopyTo(new Artist
                        {
                            EndDate = mb.EndDate,
                            BioContext = mb.Bio,
                            Profile = mb.Profile,
                            MusicBrainzId = mb.MusicBrainzId,
                            BeginDate = mb.BeginDate,
                            Name = result.Name ?? mb.ArtistName,
                            SortName = result.SortName ?? mb.ArtistSortName,
                            Thumbnail = mb.ArtistThumbnailUrl != null ? WebHelper.BytesForImageUrl(mb.ArtistThumbnailUrl) : null,
                            ArtistType = mb.ArtistType
                        });
                    }
                    if (mbResult.Errors != null)
                    {
                        resultsExceptions.AddRange(mbResult.Errors);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "MusicBrainzArtistSearch: " + ex.Serialize());
            }
            try
            {
                if (this.LastFmArtistSearchEngine.IsEnabled)
                {
                    var lastFmResult = await this.LastFmArtistSearchEngine.PerformArtistSearch(result.Name, 1);
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
                        if (l.ISNIs != null)
                        {
                            result.ISNIList = result.ISNIList.AddToDelimitedList(l.ISNIs);
                        }
                        if (l.ImageUrls != null)
                        {
                            ArtistImageUrls.AddRange(l.ImageUrls);
                        }
                        if (l.ArtistGenres != null)
                        {
                            ArtistGenres.AddRange(l.ArtistGenres);
                        }
                        if (!string.IsNullOrEmpty(l.ArtistName) && !l.ArtistName.Equals(result.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            result.AlternateNames.AddToDelimitedList(new string[] { l.ArtistName });
                        }
                        result.CopyTo(new Artist
                        {
                            EndDate = l.EndDate,
                            BioContext = this.HttpEncoder.HtmlEncode(l.Bio),
                            Profile = this.HttpEncoder.HtmlEncode(l.Profile),
                            MusicBrainzId = l.MusicBrainzId,
                            BeginDate = l.BeginDate,
                            Name = result.Name ?? l.ArtistName,
                            SortName = result.SortName ?? l.ArtistSortName,
                            Thumbnail = l.ArtistThumbnailUrl != null ? WebHelper.BytesForImageUrl(l.ArtistThumbnailUrl) : null,
                            ArtistType = result.ArtistType ?? l.ArtistType
                        });
                    }
                    if (lastFmResult.Errors != null)
                    {
                        resultsExceptions.AddRange(lastFmResult.Errors);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "LastFMArtistSearch: " + ex.Serialize());
            }
            try
            {
                if (this.SpotifyArtistSearchEngine.IsEnabled)
                {
                    var spotifyResult = await this.SpotifyArtistSearchEngine.PerformArtistSearch(result.Name, 1);
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
                            ArtistImageUrls.AddRange(s.ImageUrls);
                        }
                        if (s.ArtistGenres != null)
                        {
                            ArtistGenres.AddRange(s.ArtistGenres);
                        }
                        if (!string.IsNullOrEmpty(s.ArtistName) && !s.ArtistName.Equals(result.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            result.AlternateNames.AddToDelimitedList(new string[] { s.ArtistName });
                        }
                        result.CopyTo(new Artist
                        {
                            EndDate = s.EndDate,
                            BioContext = s.Bio,
                            Profile = this.HttpEncoder.HtmlEncode(s.Profile),
                            MusicBrainzId = s.MusicBrainzId,
                            BeginDate = s.BeginDate,
                            Name = result.Name ?? s.ArtistName,
                            SortName = result.SortName ?? s.ArtistSortName,
                            Thumbnail = s.ArtistThumbnailUrl != null ? WebHelper.BytesForImageUrl(s.ArtistThumbnailUrl) : null,
                            ArtistType = result.ArtistType ?? s.ArtistType
                        });
                    }
                    if (spotifyResult.Errors != null)
                    {
                        resultsExceptions.AddRange(spotifyResult.Errors);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "SpotifyArtistSearch: " + ex.Serialize());
            }
            try
            {
                if (this.DiscogsArtistSearchEngine.IsEnabled)
                {
                    var discogsResult = await this.DiscogsArtistSearchEngine.PerformArtistSearch(result.Name, 1);
                    if (discogsResult.IsSuccess)
                    {
                        var d = discogsResult.Data.First();
                        if (d.Urls != null)
                        {
                            result.URLs = result.URLs.AddToDelimitedList(d.Urls);
                        }
                        if (d.ImageUrls != null)
                        {
                            ArtistImageUrls.AddRange(d.ImageUrls);
                        }
                        if (d.AlternateNames != null)
                        {
                            result.AlternateNames = result.AlternateNames.AddToDelimitedList(d.AlternateNames);
                        }
                        if (!string.IsNullOrEmpty(d.ArtistName) && !d.ArtistName.Equals(result.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            result.AlternateNames.AddToDelimitedList(new string[] { d.ArtistName });
                        }
                        result.CopyTo(new Artist
                        {
                            Profile = this.HttpEncoder.HtmlEncode(d.Profile),
                            DiscogsId = d.DiscogsId,
                            Name = result.Name ?? d.ArtistName,
                            RealName = result.RealName ?? d.ArtistRealName,
                            Thumbnail = d.ArtistThumbnailUrl != null ? WebHelper.BytesForImageUrl(d.ArtistThumbnailUrl) : null,
                            ArtistType = result.ArtistType ?? d.ArtistType
                        });
                    }
                    if (discogsResult.Errors != null)
                    {
                        resultsExceptions.AddRange(discogsResult.Errors);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "DiscogsArtistSearch: " + ex.Serialize());
            }
            try
            {
                if (this.WikipediaArtistSearchEngine.IsEnabled)
                {
                    var wikiName = result.Name;
                    // Help get better results for bands with proper nouns (e.g. "Poison")
                    if (!result.ArtistType.Equals("Person", StringComparison.OrdinalIgnoreCase))
                    {
                        wikiName = wikiName + " band";
                    }
                    var wikipediaResult = await this.WikipediaArtistSearchEngine.PerformArtistSearch(wikiName, 1);
                    if (wikipediaResult != null)
                    {
                        if (wikipediaResult.IsSuccess)
                        {
                            var w = wikipediaResult.Data.First();
                            result.CopyTo(new Artist
                            {
                                BioContext = this.HttpEncoder.HtmlEncode(w.Bio)
                            });
                        }
                        if (wikipediaResult.Errors != null)
                        {
                            resultsExceptions.AddRange(wikipediaResult.Errors);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "WikipediaArtistSearch: " + ex.Serialize());
            }
            try
            {
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
                if (ArtistGenres.Any())
                {
                    var genreInfos = (from ag in ArtistGenres
                                      join g in this.DbContext.Genres on ag equals g.Name into gg
                                      from g in gg.DefaultIfEmpty()
                                      select new
                                      {
                                          newGenre = ag.ToTitleCase(),
                                          existingGenre = g
                                      });
                    result.Genres = new List<ArtistGenre>();
                    foreach (var genreInfo in genreInfos)
                    {
                        result.Genres.Add(new ArtistGenre
                        {
                            Genre = genreInfo.existingGenre != null ? genreInfo.existingGenre : new Genre
                            {
                                Name = genreInfo.newGenre
                            }
                        });
                    }
                }
                if (ArtistImageUrls.Any())
                {
                    var imageBag = new ConcurrentBag<Image>();
                    var i = ArtistImageUrls.Select(async url =>
                    {
                        imageBag.Add(await WebHelper.GetImageFromUrlAsync(url));
                    });
                    await Task.WhenAll(i);
                    result.Images = imageBag.Where(x => x != null && x.Bytes != null).GroupBy(x => x.Signature).Select(x => x.First()).Take(this.Configuration.Processing.MaximumArtistImagesToAdd).ToList();
                    if (result.Thumbnail == null && result.Images != null)
                    {
                        result.Thumbnail = result.Images.First().Bytes;
                    }
                }
                if (result.Thumbnail != null)
                {
                    result.Thumbnail = ImageHelper.ResizeImage(result.Thumbnail, this.Configuration.ThumbnailImageSize.Width, this.Configuration.ThumbnailImageSize.Height);
                    result.Thumbnail = ImageHelper.ConvertToJpegFormat(result.Thumbnail);
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "CombiningResults: " + ex.Serialize());
            }
            result.SortName = result.SortName.ToTitleCase();
            if (!string.IsNullOrEmpty(result.ArtistType))
            {
                switch (result.ArtistType.ToLower().Replace('-', ' '))
                {
                    case "Artist":
                    case "one man band":
                    case "one woman band":
                    case "solo":
                    case "person":
                        result.ArtistType = "Person";
                        break;

                    case "band":
                    case "big band":
                    case "duet":
                    case "jug band":
                    case "quartet":
                    case "quartette":
                    case "sextet":
                    case "trio":
                    case "group":
                        result.ArtistType = "Group";
                        break;

                    case "orchestra":
                        result.ArtistType = "Orchestra";
                        break;

                    case "choir band":
                    case "choir":
                        result.ArtistType = "Choir";
                        break;

                    case "movie part":
                    case "movie role":
                    case "role":
                    case "character":
                        result.ArtistType = "Character";
                        break;

                    default:
                        this.Logger.LogWarning(string.Format("Unknown Artist Type [{0}]", result.ArtistType));
                        result.ArtistType = "Other";
                        break;
                }
            }
            sw.Stop();
            return new OperationResult<Artist>
            {
                Data = result,
                IsSuccess = result != null,
                Errors = resultsExceptions,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        private Artist DatabaseQueryForArtistName(string name, string sortName = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            try
            {
                var getParams = new List<object>();
                var searchName = name.NormalizeName().ToLower();
                var searchSortName = !string.IsNullOrEmpty(sortName) ? sortName.NormalizeName().ToLower() : searchName;
                var specialSearchName = name.ToAlphanumericName();
                getParams.Add(new MySqlParameter("@isName", searchName));
                getParams.Add(new MySqlParameter("@isSortName", searchSortName));
                getParams.Add(new MySqlParameter("@startAlt", string.Format("{0}|%", searchName)));
                getParams.Add(new MySqlParameter("@inAlt", string.Format("%|{0}|%", searchName)));
                getParams.Add(new MySqlParameter("@endAlt", string.Format("%|{0}", searchName)));
                getParams.Add(new MySqlParameter("@sstartAlt", string.Format("{0}|%", specialSearchName)));
                getParams.Add(new MySqlParameter("@sinAlt", string.Format("%|{0}|%", specialSearchName)));
                getParams.Add(new MySqlParameter("@sendAlt", string.Format("%|{0}", specialSearchName)));
                return this.DbContext.Artists.FromSql(@"SELECT *
                FROM `artist`
                WHERE LCASE(name) = @isName
                OR LCASE(sortName) = @isName
                OR LCASE(sortName) = @isSortName
                OR LCASE(alternatenames) = @isName
                OR alternatenames like @startAlt
                OR alternatenames like @sstartAlt
                OR alternatenames like @inAlt
                OR alternatenames like @sinAlt
                OR (alternatenames like @endAlt
                OR alternatenames like @sendAlt)
                LIMIT 1", getParams.ToArray()).FirstOrDefault();
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Serialize());
            }
            return null;
        }
    }
}