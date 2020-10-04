using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Data.Context;
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
using System.IO;
using System.Linq;
using System.Text.Json;
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
        private readonly List<int> _addedArtistIds = new List<int>();

        public IEnumerable<int> AddedArtistIds => _addedArtistIds;

        public IArtistSearchEngine DiscogsArtistSearchEngine { get; }

        public IArtistSearchEngine ITunesArtistSearchEngine { get; }

        public IArtistSearchEngine LastFmArtistSearchEngine { get; }

        public IArtistSearchEngine MusicBrainzArtistSearchEngine { get; }

        public IArtistSearchEngine SpotifyArtistSearchEngine { get; }

        public IArtistSearchEngine WikipediaArtistSearchEngine { get; }

        public ArtistLookupEngine(
            IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IRoadieDbContext context,
            ICacheManager cacheManager,
            ILogger<ArtistLookupEngine> logger,
            musicbrainz.IMusicBrainzProvider musicBrainzProvider,
            lastfm.ILastFmHelper lastFmHelper,
            spotify.ISpotifyHelper spotifyHelper,
            wikipedia.IWikipediaHelper wikipediaHelper,
            discogs.IDiscogsHelper discogsHelper,
            IITunesSearchEngine iTunesSearchEngine)
            : base(configuration, httpEncoder, context, cacheManager, logger)
        {
            ITunesArtistSearchEngine = iTunesSearchEngine;
            MusicBrainzArtistSearchEngine = musicBrainzProvider;
            LastFmArtistSearchEngine = lastFmHelper;
            SpotifyArtistSearchEngine = spotifyHelper;
            WikipediaArtistSearchEngine = wikipediaHelper;
            DiscogsArtistSearchEngine = discogsHelper;
        }

        public async Task<OperationResult<Artist>> Add(Artist artist)
        {
            var sw = Stopwatch.StartNew();

            SimpleContract.Requires<ArgumentNullException>(artist != null, "Invalid Artist");

            try
            {
                var artistGenreTables = artist.Genres;
                var artistImages = artist.Images ?? new List<Image>();
                var now = DateTime.UtcNow;
                artist.AlternateNames = artist.AlternateNames.AddToDelimitedList(new[] { artist.Name.ToAlphanumericName() });
                artist.Genres = null;
                artist.Images = null;
                if (!artist.IsValid)
                {
                    return new OperationResult<Artist>
                    {
                        Errors = new Exception[1] { new Exception("Artist is Invalid") }
                    };
                }
                var addArtistResult = DbContext.Artists.Add(artist);
                var inserted = await DbContext.SaveChangesAsync().ConfigureAwait(false);
                _addedArtistIds.Add(artist.Id);
                if (artist.Id < 1 && addArtistResult.Entity.Id > 0) artist.Id = addArtistResult.Entity.Id;
                if (inserted > 0 && artist.Id > 0)
                {
                    if (artistGenreTables != null)
                    {
                        foreach (var artistGenreTable in artistGenreTables.Where(x => x?.Genre?.Name != null).Select(x => x.Genre?.Name).Distinct())
                        {
                            var genreName = artistGenreTable.ToAlphanumericName().ToTitleCase();
                            var normalizedName = genreName.ToUpper();
                            if (string.IsNullOrEmpty(genreName)) continue;
                            if (genreName.Length > 100)
                            {
                                var originalName = genreName;
                                genreName = genreName.Substring(0, 99);
                                Logger.LogWarning($"Genre Name Too long was [{originalName}] truncated to [{genreName}]");
                            }
                            var genre = DbContext.Genres.FirstOrDefault(x => x.NormalizedName == normalizedName);
                            if (genre == null)
                            {
                                genre = new Genre
                                {
                                    Name = genreName,
                                    NormalizedName = normalizedName
                                };
                                DbContext.Genres.Add(genre);
                                await DbContext.SaveChangesAsync().ConfigureAwait(false);
                            }
                            DbContext.ArtistGenres.Add(new ArtistGenre
                            {
                                ArtistId = artist.Id,
                                GenreId = genre.Id
                            });
                            await DbContext.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }

                    if (artistImages.Any(x => x.Status == Statuses.New))
                    {
                        var artistFolder = artist.ArtistFileFolder(Configuration, true);
                        var looper = -1;
                        string releaseImageFilename;
                        foreach (var artistImage in artistImages)
                        {
                            if (!(artistImage?.Bytes.Length > 0))
                            {
                                continue;
                            }
                            artistImage.Bytes = ImageHelper.ConvertToJpegFormat(artistImage.Bytes);
                            if (looper == -1)
                            {
                                releaseImageFilename = Path.Combine(artistFolder, ImageHelper.ArtistImageFilename);
                            }
                            else
                            {
                                releaseImageFilename = Path.Combine(artistFolder, string.Format(ImageHelper.ArtistSecondaryImageFilename, looper.ToString("00")));
                            }
                            while (File.Exists(releaseImageFilename))
                            {
                                looper++;
                                releaseImageFilename = Path.Combine(artistFolder, string.Format(ImageHelper.ArtistSecondaryImageFilename, looper.ToString("00")));
                            }
                            File.WriteAllBytes(releaseImageFilename, artistImage.Bytes);
                            looper++;
                        }
                    }
                    sw.Stop();
                    Logger.LogTrace($"Added New Artist: Elapsed Time [{ sw.ElapsedMilliseconds }], Artist `{ artist }`");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error Adding Artist `{ artist }`, Ex [{ ex.Serialize() }]");
            }
            return new OperationResult<Artist>
            {
                IsSuccess = artist.Id > 0,
                Data = artist,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<IEnumerable<Artist>> DatabaseQueryForArtistName(string name, string sortName = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            try
            {
                var searchName = name.NormalizeName().ToLower();
                var searchSortName = !string.IsNullOrEmpty(sortName) ? sortName.NormalizeName().ToLower() : searchName;
                var specialSearchName = name.ToAlphanumericName();

                var searchNameStart = $"{searchName}|";
                var searchNameIn = $"|{searchName}|";
                var searchNameEnd = $"|{searchName}";

                var specialSearchNameStart = $"{specialSearchName}|";
                var specialSearchNameIn = $"|{specialSearchName}|";
                var specialSearchNameEnd = $"|{specialSearchName}";

                return await (from a in DbContext.Artists
                              where a.Name.ToLower() == searchName ||
                                    a.Name.ToLower() == specialSearchName ||
                                    a.SortName.ToLower() == searchName ||
                                    a.SortName.ToLower() == searchSortName ||
                                    a.SortName.ToLower() == specialSearchName ||
                                    a.AlternateNames.ToLower().Equals(searchName) ||
                                    a.AlternateNames.ToLower().StartsWith(searchNameStart) ||
                                    a.AlternateNames.ToLower().Contains(searchNameIn) ||
                                    a.AlternateNames.ToLower().EndsWith(searchNameEnd) ||
                                    a.AlternateNames.ToLower().Equals(specialSearchName) ||
                                    a.AlternateNames.ToLower().StartsWith(specialSearchNameStart) ||
                                    a.AlternateNames.ToLower().Contains(specialSearchNameIn) ||
                                    a.AlternateNames.ToLower().EndsWith(specialSearchNameEnd)
                              select a
                    ).ToArrayAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
            }

            return null;
        }

        public async Task<OperationResult<Artist>> GetByName(AudioMetaData metaData, bool doFindIfNotInDatabase = false)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                var artistName = metaData.Artist ?? metaData.TrackArtist;
                var cacheRegion = new Artist { Name = artistName }.CacheRegion;
                var cacheKey = string.Format("urn:artist_by_name:{0}", artistName);
                var resultInCache = CacheManager.Get<Artist>(cacheKey, cacheRegion);
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

                // See if roadie.json file exists in the metadata files folder, if so then use artist data from that
                string releaseRoadieDataFilename = null;
                try
                {
                    releaseRoadieDataFilename = Path.Combine(Path.GetDirectoryName(metaData.Filename), "roadie.artist.json");
                }
                catch (Exception)
                {
                }

                if (!string.IsNullOrEmpty(releaseRoadieDataFilename) && File.Exists(releaseRoadieDataFilename))
                {
                    var artistFromJson = CacheManager.CacheSerializer.Deserialize<Artist>(File.ReadAllText(releaseRoadieDataFilename));
                    artistName = artistFromJson?.Name;
                }

                var artist = (await DatabaseQueryForArtistName(artistName).ConfigureAwait(false)).FirstOrDefault();
                sw.Stop();
                if (artist?.IsValid != true)
                {
                    Logger.LogTrace("ArtistLookupEngine: Artist Not Found By Name [{0}]", artistName);
                    if (doFindIfNotInDatabase)
                    {
                        OperationResult<Artist> artistSearch = null;
                        if (!string.IsNullOrEmpty(releaseRoadieDataFilename) && File.Exists(releaseRoadieDataFilename))
                        {
                            artist = CacheManager.CacheSerializer.Deserialize<Artist>(File.ReadAllText(releaseRoadieDataFilename));
                            var addResult = await Add(artist).ConfigureAwait(false);
                            if (!addResult.IsSuccess)
                            {
                                sw.Stop();
                                Logger.LogWarning("Unable To Add Artist For Roadie Data File [{0}]", releaseRoadieDataFilename);
                                return new OperationResult<Artist>
                                {
                                    OperationTime = sw.ElapsedMilliseconds,
                                    Errors = addResult.Errors
                                };
                            }
                            else
                            {
                                File.Delete(releaseRoadieDataFilename);
                            }
                            artist = addResult.Data;
                        }
                        else
                        {
                            try
                            {
                                artistSearch = await PerformMetaDataProvidersArtistSearch(metaData).ConfigureAwait(false);
                                if (artistSearch.IsSuccess)
                                {
                                    artist = artistSearch.Data;
                                    // See if Artist already exist with either Name or Sort Name
                                    var alreadyExists = (await DatabaseQueryForArtistName(artistSearch.Data.Name, artistSearch.Data.SortNameValue).ConfigureAwait(false)).FirstOrDefault();
                                    if (alreadyExists?.IsValid != true)
                                    {
                                        var addResult = await Add(artist).ConfigureAwait(false);
                                        if (!addResult.IsSuccess)
                                        {
                                            sw.Stop();
                                            Logger.LogWarning("Unable To Add Artist For MetaData [{0}]", metaData.ToString());
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
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, ex.Serialize());
                            }
                        }
                    }
                }

                if (artist?.IsValid == true)
                {
                    CacheManager.Add(cacheKey, artist);
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
                Logger.LogError(ex, ex.Serialize());
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
            var artistGenres = new List<string>();
            var artistImageUrls = new List<string>();
            var artistName = metaData.Artist;

            try
            {
                if (ITunesArtistSearchEngine.IsEnabled)
                {
                    var sw2 = Stopwatch.StartNew();
                    var iTunesResult = await ITunesArtistSearchEngine.PerformArtistSearchAsync(artistName, 1).ConfigureAwait(false);
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
                            result.ISNI = result.ISNI.AddToDelimitedList(i.ISNIs);
                        }
                        if (i.ImageUrls != null)
                        {
                            artistImageUrls.AddRange(i.ImageUrls);
                        }
                        if (i.ArtistGenres != null)
                        {
                            artistGenres.AddRange(i.ArtistGenres);
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
                            ArtistType = result.ArtistType ?? i.ArtistType
                        });
                    }

                    if (iTunesResult.Errors != null) resultsExceptions.AddRange(iTunesResult.Errors);

                    sw2.Stop();
                    Logger.LogTrace($"PerformMetaDataProvidersArtistSearch: ITunesArtistSearchEngine Complete [{ sw2.ElapsedMilliseconds }]");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "iTunesArtistSearch: " + ex.Serialize());
            }

            try
            {
                if (MusicBrainzArtistSearchEngine.IsEnabled)
                {
                    var sw2 = Stopwatch.StartNew();
                    var mbResult = await MusicBrainzArtistSearchEngine.PerformArtistSearchAsync(result.Name, 1).ConfigureAwait(false);
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
                            result.ISNI = result.ISNI.AddToDelimitedList(mb.ISNIs);
                        }
                        if (mb.ImageUrls != null)
                        {
                            artistImageUrls.AddRange(mb.ImageUrls);
                        }
                        if (mb.ArtistGenres != null)
                        {
                            artistGenres.AddRange(mb.ArtistGenres);
                        }
                        if (!string.IsNullOrEmpty(mb.ArtistName) &&
                            !mb.ArtistName.Equals(result.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            result.AlternateNames.AddToDelimitedList(new[] { mb.ArtistName });
                        }
                        result.CopyTo(new Artist
                        {
                            EndDate = mb.EndDate,
                            BioContext = mb.Bio,
                            Profile = mb.Profile,
                            MusicBrainzId = mb.MusicBrainzId,
                            DiscogsId = mb.DiscogsId,
                            SpotifyId = mb.SpotifyId,
                            ITunesId = mb.iTunesId,
                            AmgId = mb.AmgId,
                            BeginDate = mb.BeginDate,
                            Name = result.Name ?? mb.ArtistName,
                            SortName = result.SortName ?? mb.ArtistSortName,
                            ArtistType = mb.ArtistType
                        });
                    }

                    if (mbResult.Errors != null) resultsExceptions.AddRange(mbResult.Errors);
                    sw2.Stop();
                    Logger.LogTrace($"PerformMetaDataProvidersArtistSearch: MusicBrainzArtistSearchEngine Complete [{ sw2.ElapsedMilliseconds }]");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "MusicBrainzArtistSearch: " + ex.Serialize());
            }

            try
            {
                if (LastFmArtistSearchEngine.IsEnabled)
                {
                    var sw2 = Stopwatch.StartNew();
                    var lastFmResult = await LastFmArtistSearchEngine.PerformArtistSearchAsync(result.Name, 1).ConfigureAwait(false);
                    if (lastFmResult.IsSuccess)
                    {
                        var l = lastFmResult.Data.First();
                        if (l.AlternateNames != null)
                            result.AlternateNames = result.AlternateNames.AddToDelimitedList(l.AlternateNames);
                        if (l.Tags != null) result.Tags = result.Tags.AddToDelimitedList(l.Tags);
                        if (l.Urls != null) result.URLs = result.URLs.AddToDelimitedList(l.Urls);
                        if (l.ISNIs != null) result.ISNI = result.ISNI.AddToDelimitedList(l.ISNIs);
                        if (l.ImageUrls != null) artistImageUrls.AddRange(l.ImageUrls);
                        if (l.ArtistGenres != null) artistGenres.AddRange(l.ArtistGenres);
                        if (!string.IsNullOrEmpty(l.ArtistName) &&
                            !l.ArtistName.Equals(result.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            result.AlternateNames.AddToDelimitedList(new[] { l.ArtistName });
                        }
                        result.CopyTo(new Artist
                        {
                            EndDate = l.EndDate,
                            BioContext = HttpEncoder.HtmlEncode(l.Bio),
                            Profile = HttpEncoder.HtmlEncode(l.Profile),
                            MusicBrainzId = l.MusicBrainzId,
                            BeginDate = l.BeginDate,
                            Name = result.Name ?? l.ArtistName,
                            SortName = result.SortName ?? l.ArtistSortName,
                            ArtistType = result.ArtistType ?? l.ArtistType
                        });
                    }

                    if (lastFmResult.Errors != null) resultsExceptions.AddRange(lastFmResult.Errors);
                    sw2.Stop();
                    Logger.LogTrace($"PerformMetaDataProvidersArtistSearch: LastFmArtistSearchEngine Complete [{ sw2.ElapsedMilliseconds }]");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LastFMArtistSearch: " + ex.Serialize());
            }

            try
            {
                if (SpotifyArtistSearchEngine.IsEnabled)
                {
                    var sw2 = Stopwatch.StartNew();
                    var spotifyResult = await SpotifyArtistSearchEngine.PerformArtistSearchAsync(result.Name, 1).ConfigureAwait(false);
                    if (spotifyResult.IsSuccess)
                    {
                        var s = spotifyResult.Data.First();
                        if (s.Tags != null) result.Tags = result.Tags.AddToDelimitedList(s.Tags);
                        if (s.Urls != null) result.URLs = result.URLs.AddToDelimitedList(s.Urls);
                        if (s.ImageUrls != null) artistImageUrls.AddRange(s.ImageUrls);
                        if (s.ArtistGenres != null) artistGenres.AddRange(s.ArtistGenres);
                        if (!string.IsNullOrEmpty(s.ArtistName) &&
                            !s.ArtistName.Equals(result.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            result.AlternateNames.AddToDelimitedList(new[] { s.ArtistName });
                        }
                        result.CopyTo(new Artist
                        {
                            EndDate = s.EndDate,
                            BioContext = s.Bio,
                            Profile = HttpEncoder.HtmlEncode(s.Profile),
                            MusicBrainzId = s.MusicBrainzId,
                            SpotifyId = s.SpotifyId,
                            BeginDate = s.BeginDate,
                            Name = result.Name ?? s.ArtistName,
                            SortName = result.SortName ?? s.ArtistSortName,
                            ArtistType = result.ArtistType ?? s.ArtistType
                        });
                    }

                    if (spotifyResult.Errors != null) resultsExceptions.AddRange(spotifyResult.Errors);
                    sw2.Stop();
                    Logger.LogTrace($"PerformMetaDataProvidersArtistSearch: SpotifyArtistSearchEngine Complete [{ sw2.ElapsedMilliseconds }]");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SpotifyArtistSearch: " + ex.Serialize());
            }

            try
            {
                if (DiscogsArtistSearchEngine.IsEnabled)
                {
                    var sw2 = Stopwatch.StartNew();
                    var discogsResult = await DiscogsArtistSearchEngine.PerformArtistSearchAsync(result.Name, 1).ConfigureAwait(false);
                    if (discogsResult.IsSuccess)
                    {
                        var d = discogsResult?.Data?.FirstOrDefault();
                        if (d != null)
                        {
                            if (d.Urls != null)
                            {
                                result.URLs = result.URLs.AddToDelimitedList(d.Urls);
                            }
                            if (d.ImageUrls != null)
                            {
                                artistImageUrls.AddRange(d.ImageUrls);
                            }
                            if (d.AlternateNames != null)
                            {
                                result.AlternateNames = result.AlternateNames.AddToDelimitedList(d.AlternateNames);
                            }
                            if (!string.IsNullOrEmpty(d.ArtistName) &&
                                !d.ArtistName.Equals(result.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                result.AlternateNames.AddToDelimitedList(new[] { d.ArtistName });
                            }
                            result.CopyTo(new Artist
                            {
                                Profile = HttpEncoder.HtmlEncode(d.Profile),
                                DiscogsId = d.DiscogsId,
                                Name = result.Name ?? d.ArtistName,
                                RealName = result.RealName ?? d.ArtistRealName,
                                ArtistType = result.ArtistType ?? d.ArtistType
                            });
                        }
                    }
                    if (discogsResult.Errors != null)
                    {
                        resultsExceptions.AddRange(discogsResult.Errors);
                    }
                    sw2.Stop();
                    Logger.LogTrace($"PerformMetaDataProvidersArtistSearch: DiscogsArtistSearchEngine Complete [{ sw2.ElapsedMilliseconds }]");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DiscogsArtistSearch: " + ex.Serialize());
            }

            try
            {
                if (WikipediaArtistSearchEngine.IsEnabled)
                {
                    var sw2 = Stopwatch.StartNew();
                    var wikiName = result?.Name;
                    // Help get better results for bands with proper nouns (e.g. "Poison" vs "Poison Band")
                    if (!(result?.ArtistType ?? string.Empty).Equals("Person", StringComparison.OrdinalIgnoreCase))
                    {
                        wikiName += " band";
                    }
                    var wikipediaResult = await WikipediaArtistSearchEngine.PerformArtistSearchAsync(wikiName, 1).ConfigureAwait(false);
                    if (wikipediaResult?.Data != null)
                    {
                        if (wikipediaResult.IsSuccess)
                        {
                            var w = wikipediaResult?.Data?.FirstOrDefault();
                            if (!string.IsNullOrEmpty(w?.Bio))
                            {
                                result.CopyTo(new Artist
                                {
                                    BioContext = HttpEncoder.HtmlEncode(w.Bio)
                                });
                            }
                        }

                        if (wikipediaResult.Errors != null) resultsExceptions.AddRange(wikipediaResult.Errors);
                    }
                    sw2.Stop();
                    Logger.LogTrace($"PerformMetaDataProvidersArtistSearch: WikipediaArtistSearchEngine Complete [{ sw2.ElapsedMilliseconds }]");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "WikipediaArtistSearch: " + ex.Serialize());
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
                if (artistGenres.Count > 0)
                {
                    var sw2 = Stopwatch.StartNew();
                    var genreInfos = from ag in artistGenres
                                     join g in DbContext.Genres on ag equals g.Name into gg
                                     from g in gg.DefaultIfEmpty()
                                     select new
                                     {
                                         newGenre = ag.ToTitleCase(),
                                         existingGenre = g
                                     };
                    result.Genres = new List<ArtistGenre>();
                    foreach (var genreInfo in genreInfos)
                    {
                        var ag = new ArtistGenre
                        {
                            Genre = genreInfo.existingGenre ?? new Genre
                            {
                                Name = genreInfo.newGenre,
                                NormalizedName = genreInfo.newGenre.ToAlphanumericName()
                            }
                        };
                        if (!result.Genres.Any(x => x.Genre.NormalizedName == ag.Genre.NormalizedName))
                        {
                            result.Genres.Add(ag);
                        }
                    }
                    sw2.Stop();
                    Logger.LogTrace($"PerformMetaDataProvidersArtistSearch: Artist Genre Processing Complete [{ sw2.ElapsedMilliseconds }]");
                }

                if (artistImageUrls.Count > 0)
                {
                    var sw2 = Stopwatch.StartNew();
                    var imageBag = new ConcurrentBag<IImage>();
                    var i = artistImageUrls.Select(async url => imageBag.Add(await WebHelper.GetImageFromUrlAsync(url).ConfigureAwait(false)));
                    await Task.WhenAll(i).ConfigureAwait(false);
                    result.Images = imageBag.Where(x => x?.Bytes != null)
                                            .GroupBy(x => x.Signature)
                                            .Select(x => x.First())
                                            .Take(Configuration.Processing.MaximumArtistImagesToAdd)
                                            .ToList();
                    sw2.Stop();
                    Logger.LogTrace($"PerformMetaDataProvidersArtistSearch: Image Url Processing Complete [{ sw2.ElapsedMilliseconds }]");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "CombiningResults: " + ex.Serialize());
            }

            result.SortName = result.SortName.ToTitleCase();
            if (!string.IsNullOrEmpty(result.ArtistType))
            {
                switch (result.ArtistType.ToLower().Replace('-', ' '))
                {
                    case "artist":
                    case "artists":
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
                        Logger.LogWarning(string.Format("Unknown Artist Type [{0}]", result.ArtistType));
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
    }
}