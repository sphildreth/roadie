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
using Roadie.Library.Processors;
using Roadie.Library.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.Factories
{
    #pragma warning disable EF1000
    public sealed class ArtistFactory : FactoryBase
    {
        private List<int> _addedArtistIds = new List<int>();
        private ReleaseFactory _releaseFactory = null;

        public IEnumerable<int> AddedArtistIds
        {
            get
            {
                return this._addedArtistIds;
            }
        }

        private ReleaseFactory ReleaseFactory
        {
            get
            {
                return this._releaseFactory;
            }
        }

        public ArtistFactory(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context,
                             ICacheManager cacheManager, ILogger logger, ReleaseFactory releaseFactory = null)
            : base(configuration, context, cacheManager, logger, httpEncoder)
        {
            this._releaseFactory = releaseFactory ?? new ReleaseFactory(configuration, httpEncoder, context, CacheManager, logger, null, this);
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
                            artist.Thumbnail = ImageHelper.ResizeImage(artist.Thumbnail, this.Configuration.Thumbnails.Width, this.Configuration.Thumbnails.Height);
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
                this._addedArtistIds.Add(artist.Id);
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
                                    sql = string.Format("INSERT INTO `ArtistGenreTable` (ArtistId, genreId) VALUES ({0}, {1});", artist.Id, genre.Id);
                                    await this.DbContext.Database.ExecuteSqlCommandAsync(sql);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            this._logger.LogError(ex, "Sql [" + sql + "] Exception [" + ex.Serialize() + "]");
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

        public async Task<OperationResult<bool>> Delete(Guid RoadieId)
        {
            var isSuccess = false;
            var Artist = this.DbContext.Artists.FirstOrDefault(x => x.RoadieId == RoadieId);
            if (Artist != null)
            {
                return await this.Delete(Artist);
            }
            return new OperationResult<bool>
            {
                Data = isSuccess
            };
        }

        public async Task<OperationResult<bool>> Delete(Artist Artist)
        {
            var isSuccess = false;
            try
            {
                if (Artist != null)
                {
                    this.DbContext.Artists.Remove(Artist);
                    await this.DbContext.SaveChangesAsync();
                    this._cacheManager.ClearRegion(Artist.CacheRegion);
                    this.Logger.LogInformation(string.Format("x DeleteArtist [{0}]", Artist.Id));
                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Serialize());
                return new OperationResult<bool>
                {
                    Errors = new Exception[1] { ex }
                };
            }
            return new OperationResult<bool>
            {
                IsSuccess = isSuccess,
                Data = isSuccess
            };
        }

        public OperationResult<Artist> GetByExternalIds(string musicBrainzId = null, string iTunesId = null, string amgId = null, string spotifyId = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            var Artist = (from a in this.DbContext.Artists
                          where ((a.MusicBrainzId != null && (musicBrainzId != null && a.MusicBrainzId == musicBrainzId)) ||
                                 (a.ITunesId != null || (iTunesId != null && a.ITunesId == iTunesId)) ||
                                 (a.AmgId != null || (amgId != null && a.AmgId == amgId)) ||
                                 (a.SpotifyId != null || (spotifyId != null && a.SpotifyId == spotifyId)))
                          select a).FirstOrDefault();
            sw.Stop();
            if (Artist == null || !Artist.IsValid)
            {
                this._logger.LogTrace("ArtistFactory: Artist Not Found By External Ids: MusicbrainzId [{0}], iTunesIs [{1}], AmgId [{2}], SpotifyId [{3}]", musicBrainzId, iTunesId, amgId, spotifyId);
            }
            return new OperationResult<Artist>
            {
                IsSuccess = Artist != null,
                OperationTime = sw.ElapsedMilliseconds,
                Data = Artist
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
                var Artist = this.DatabaseQueryForArtistName(ArtistName);
                sw.Stop();
                if (Artist == null || !Artist.IsValid)
                {
                    this._logger.LogInformation("ArtistFactory: Artist Not Found By Name [{0}]", ArtistName);
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
                            Artist = ArtistSearch.Data;
                            // See if Artist already exist with either Name or Sort Name
                            var alreadyExists = this.DatabaseQueryForArtistName(ArtistSearch.Data.Name, ArtistSearch.Data.SortNameValue);
                            if (alreadyExists == null || !alreadyExists.IsValid)
                            {
                                var addResult = await this.Add(Artist);
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
                                Artist = addResult.Data;
                            }
                            else
                            {
                                Artist = alreadyExists;
                            }
                        }
                    }
                }
                if (Artist != null && Artist.IsValid)
                {
                    this.CacheManager.Add(cacheKey, Artist);
                }
                return new OperationResult<Artist>
                {
                    IsSuccess = Artist != null,
                    OperationTime = sw.ElapsedMilliseconds,
                    Data = Artist
                };
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Serialize());
            }
            return new OperationResult<Artist>();
        }

        /// <summary>
        /// Merge one Artist into another one
        /// </summary>
        /// <param name="ArtistToMerge">The Artist to be merged</param>
        /// <param name="ArtistToMergeInto">The Artist to merge into</param>
        /// <returns></returns>
        public async Task<OperationResult<Artist>> MergeArtists(Artist ArtistToMerge, Artist ArtistToMergeInto, bool doDbUpdates = false)
        {
            SimpleContract.Requires<ArgumentNullException>(ArtistToMerge != null, "Invalid Artist");
            SimpleContract.Requires<ArgumentNullException>(ArtistToMergeInto != null, "Invalid Artist");

            var result = false;
            var now = DateTime.UtcNow;

            var sw = new Stopwatch();
            sw.Start();

            ArtistToMergeInto.RealName = ArtistToMerge.RealName ?? ArtistToMergeInto.RealName;
            ArtistToMergeInto.MusicBrainzId = ArtistToMerge.MusicBrainzId ?? ArtistToMergeInto.MusicBrainzId;
            ArtistToMergeInto.ITunesId = ArtistToMerge.ITunesId ?? ArtistToMergeInto.ITunesId;
            ArtistToMergeInto.AmgId = ArtistToMerge.AmgId ?? ArtistToMergeInto.AmgId;
            ArtistToMergeInto.SpotifyId = ArtistToMerge.SpotifyId ?? ArtistToMergeInto.SpotifyId;
            ArtistToMergeInto.Thumbnail = ArtistToMerge.Thumbnail ?? ArtistToMergeInto.Thumbnail;
            ArtistToMergeInto.Profile = ArtistToMerge.Profile ?? ArtistToMergeInto.Profile;
            ArtistToMergeInto.BirthDate = ArtistToMerge.BirthDate ?? ArtistToMergeInto.BirthDate;
            ArtistToMergeInto.BeginDate = ArtistToMerge.BeginDate ?? ArtistToMergeInto.BeginDate;
            ArtistToMergeInto.EndDate = ArtistToMerge.EndDate ?? ArtistToMergeInto.EndDate;
            if (!string.IsNullOrEmpty(ArtistToMerge.ArtistType) && !ArtistToMerge.ArtistType.Equals("Other", StringComparison.OrdinalIgnoreCase))
            {
                ArtistToMergeInto.ArtistType = ArtistToMerge.ArtistType;
            }
            ArtistToMergeInto.BioContext = ArtistToMerge.BioContext ?? ArtistToMergeInto.BioContext;
            ArtistToMergeInto.DiscogsId = ArtistToMerge.DiscogsId ?? ArtistToMergeInto.DiscogsId;

            ArtistToMergeInto.Tags = ArtistToMergeInto.Tags.AddToDelimitedList(ArtistToMerge.Tags.ToListFromDelimited());
            var altNames = ArtistToMerge.AlternateNames.ToListFromDelimited().ToList();
            altNames.Add(ArtistToMerge.Name);
            altNames.Add(ArtistToMerge.SortName);
            ArtistToMergeInto.AlternateNames = ArtistToMergeInto.AlternateNames.AddToDelimitedList(altNames);
            ArtistToMergeInto.URLs = ArtistToMergeInto.URLs.AddToDelimitedList(ArtistToMerge.URLs.ToListFromDelimited());
            ArtistToMergeInto.ISNIList = ArtistToMergeInto.ISNIList.AddToDelimitedList(ArtistToMerge.ISNIList.ToListFromDelimited());
            ArtistToMergeInto.LastUpdated = now;

            if (doDbUpdates)
            {
                string sql = null;

                sql = "UPDATE `ArtistGenreTable` set ArtistId = " + ArtistToMergeInto.Id + " WHERE ArtistId = " + ArtistToMerge.Id + ";";
                await this.DbContext.Database.ExecuteSqlCommandAsync(sql);
                sql = "UPDATE `image` set ArtistId = " + ArtistToMergeInto.Id + " WHERE ArtistId = " + ArtistToMerge.Id + ";";
                await this.DbContext.Database.ExecuteSqlCommandAsync(sql);
                sql = "UPDATE `userArtist` set ArtistId = " + ArtistToMergeInto.Id + " WHERE ArtistId = " + ArtistToMerge.Id + ";";
                await this.DbContext.Database.ExecuteSqlCommandAsync(sql);
                sql = "UPDATE `track` set ArtistId = " + ArtistToMergeInto.Id + " WHERE ArtistId = " + ArtistToMerge.Id + ";";
                await this.DbContext.Database.ExecuteSqlCommandAsync(sql);

                try
                {
                    sql = "UPDATE `release` set ArtistId = " + ArtistToMergeInto.Id + " WHERE ArtistId = " + ArtistToMerge.Id + ";";
                    await this.DbContext.Database.ExecuteSqlCommandAsync(sql);
                }
                catch (Exception ex)
                {
                    this._logger.LogWarning(ex.ToString());
                }
                var artistFolder = ArtistToMerge.ArtistFileFolder(this.Configuration, this.Configuration.LibraryFolder);
                foreach (var release in this.DbContext.Releases.Include("Artist").Where(x => x.ArtistId == ArtistToMerge.Id).ToArray())
                {
                    var originalReleaseFolder = release.ReleaseFileFolder(artistFolder);
                    await this.ReleaseFactory.Update(release, null, originalReleaseFolder);
                }

                await this.Delete(ArtistToMerge);
            }

            result = true;

            sw.Stop();
            return new OperationResult<Artist>
            {
                Data = ArtistToMergeInto,
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
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
                    result.Thumbnail = ImageHelper.ResizeImage(result.Thumbnail, this.Configuration.Thumbnails.Width, this.Configuration.Thumbnails.Height);
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

        /// <summary>
        /// Perform a Metadata Provider search and then merge the results into the given Artist
        /// </summary>
        /// <param name="ArtistId">Given Artist RoadieId</param>
        /// <returns>Operation Result</returns>
        public async Task<OperationResult<bool>> RefreshArtistMetadata(Guid ArtistId)
        {
            SimpleContract.Requires<ArgumentOutOfRangeException>(ArtistId != Guid.Empty, "Invalid ArtistId");

            var result = true;
            var resultErrors = new List<Exception>();
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                var Artist = this.DbContext.Artists.FirstOrDefault(x => x.RoadieId == ArtistId);
                if (Artist == null)
                {
                    this.Logger.LogWarning("Unable To Find Artist [{0}]", ArtistId);
                    return new OperationResult<bool>();
                }

                OperationResult<Artist> ArtistSearch = null;
                try
                {
                    ArtistSearch = await this.PerformMetaDataProvidersArtistSearch(new AudioMetaData
                    {
                        Artist = Artist.Name
                    });
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, ex.Serialize());
                }
                if (ArtistSearch.IsSuccess)
                {
                    // Do metadata search for Artist like if new Artist then set some overides and merge
                    var mergeResult = await this.MergeArtists(ArtistSearch.Data, Artist);
                    if (mergeResult.IsSuccess)
                    {
                        Artist = mergeResult.Data;
                        await this.DbContext.SaveChangesAsync();
                        sw.Stop();
                        this.CacheManager.ClearRegion(Artist.CacheRegion);
                        this.Logger.LogInformation("Scanned RefreshArtistMetadata [{0}], OperationTime [{1}]", Artist.ToString(), sw.ElapsedMilliseconds);
                    }
                    else
                    {
                        sw.Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Serialize());
                resultErrors.Add(ex);
            }
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                Errors = resultErrors,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> ScanArtistReleasesFolders(Guid artistId, string destinationFolder, bool doJustInfo)
        {
            SimpleContract.Requires<ArgumentOutOfRangeException>(artistId == Guid.Empty, "Invalid ArtistId");

            var result = true;
            var resultErrors = new List<Exception>();
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                var Artist = this.DbContext.Artists.Include("releases").FirstOrDefault(x => x.RoadieId == artistId);
                if (Artist == null)
                {
                    this.Logger.LogWarning("Unable To Find Artist [{0}]", artistId);
                    return new OperationResult<bool>();
                }
                var releaseScannedCount = 0;
                var ArtistFolder = Artist.ArtistFileFolder(this.Configuration, destinationFolder);
                var scannedArtistFolders = new List<string>();
                // Scan known releases for changes
                if (Artist.Releases != null)
                {
                    foreach (var release in Artist.Releases)
                    {
                        try
                        {
                            result = result && (await this.ReleaseFactory.ScanReleaseFolder(Guid.Empty, destinationFolder, doJustInfo, release)).Data;
                            releaseScannedCount++;
                            scannedArtistFolders.Add(release.ReleaseFileFolder(ArtistFolder));
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogError(ex, ex.Serialize());
                        }
                    }
                }
                // Any folder found in Artist folder not already scanned scan
                var folderProcessor = new FolderProcessor(this.Configuration, this.HttpEncoder, destinationFolder, this.DbContext, this.CacheManager, this.Logger);
                var nonReleaseFolders = (from d in Directory.EnumerateDirectories(ArtistFolder)
                                         where !(from r in scannedArtistFolders select r).Contains(d)
                                         orderby d
                                         select d);
                foreach (var folder in nonReleaseFolders)
                {
                    await folderProcessor.Process(new DirectoryInfo(folder), doJustInfo);
                }
                if (!doJustInfo)
                {
                    folderProcessor.DeleteEmptyFolders(new DirectoryInfo(ArtistFolder));
                }
                sw.Stop();
                this.CacheManager.ClearRegion(Artist.CacheRegion);
                this.Logger.LogInformation("Scanned Artist [{0}], Releases Scanned [{1}], OperationTime [{2}]", Artist.ToString(), releaseScannedCount, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Serialize());
                resultErrors.Add(ex);
            }
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                Errors = resultErrors,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<Artist>> Update(Artist Artist, IEnumerable<Image> ArtistImages, string destinationFolder = null)
        {
            SimpleContract.Requires<ArgumentNullException>(Artist != null, "Invalid Artist");

            var sw = new Stopwatch();
            sw.Start();

            var artistGenreTables = Artist.Genres.Select(x => new ArtistGenre { ArtistId = Artist.Id, GenreId = x.GenreId }).ToList();
            var artistAssociatedWith = Artist.AssociatedArtists.Select(x => new ArtistAssociation { ArtistId = Artist.Id, AssociatedArtistId = x.AssociatedArtistId }).ToList();
            var result = true;

            var now = DateTime.UtcNow;
            var originalArtistFolder = Artist.ArtistFileFolder(this.Configuration, destinationFolder ?? this.Configuration.LibraryFolder);
            var originalName = Artist.Name;
            var originalSortName = Artist.SortName;

            Artist.LastUpdated = now;
            await this.DbContext.SaveChangesAsync();

            this.DbContext.ArtistGenres.RemoveRange((from at in this.DbContext.ArtistGenres
                                                     where at.ArtistId == Artist.Id
                                                     select at));
            Artist.Genres = artistGenreTables;
            this.DbContext.ArtistAssociations.RemoveRange((from at in this.DbContext.ArtistAssociations
                                                           where at.ArtistId == Artist.Id
                                                           select at));
            Artist.AssociatedArtists = artistAssociatedWith;
            await this.DbContext.SaveChangesAsync();

            var existingImageIds = (from ai in ArtistImages
                                    where ai.Status != Statuses.New
                                    select ai.RoadieId).ToArray();
            this.DbContext.Images.RemoveRange((from i in this.DbContext.Images
                                               where i.ArtistId == Artist.Id
                                               where !(from x in existingImageIds select x).Contains(i.RoadieId)
                                               select i));
            await this.DbContext.SaveChangesAsync();
            if (ArtistImages != null && ArtistImages.Any(x => x.Status == Statuses.New))
            {
                foreach (var ArtistImage in ArtistImages.Where(x => x.Status == Statuses.New))
                {
                    this.DbContext.Images.Add(ArtistImage);
                }
                try
                {
                    await this.DbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, ex.Serialize());
                }
            }

            var newArtistFolder = Artist.ArtistFileFolder(this.Configuration, destinationFolder ?? this.Configuration.LibraryFolder);
            if (!originalArtistFolder.Equals(newArtistFolder, StringComparison.OrdinalIgnoreCase))
            {
                this.Logger.LogTrace("Moving Artist From Folder [{0}] To  [{1}]", originalArtistFolder, newArtistFolder);
                //  Directory.Move(originalArtistFolder, Artist.ArtistFileFolder(destinationFolder ?? SettingsHelper.Instance.LibraryFolder));
                // TODO if name changed then update Artist track files to have new Artist name
            }
            this._cacheManager.ClearRegion(Artist.CacheRegion);
            sw.Stop();

            return new OperationResult<Artist>
            {
                Data = Artist,
                IsSuccess = result,
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
                FROM `Artist`
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
                LIMIT 1;", getParams.ToArray()).FirstOrDefault();
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Serialize());
            }
            return null;
        }
    }
}