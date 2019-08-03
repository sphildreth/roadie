using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

        public ArtistLookupEngine(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context,
                                  ICacheManager cacheManager, ILogger<ArtistLookupEngine> logger, musicbrainz.IMusicBrainzProvider musicBrainzProvider,
                                  lastfm.ILastFmHelper lastFmHelper, spotify.ISpotifyHelper spotifyHelper, wikipedia.IWikipediaHelper wikipediaHelper,
                                  discogs.IDiscogsHelper discogsHelper, IITunesSearchEngine iTunesSearchEngine)
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
                var ArtistImages = artist.Images;
                var now = DateTime.UtcNow;
                artist.AlternateNames = artist.AlternateNames.AddToDelimitedList(new[] { artist.Name.ToAlphanumericName() });
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
                            artist.Thumbnail = ImageHelper.ResizeToThumbnail(artist.Thumbnail, Configuration);
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
                if (artist.Thumbnail != null)
                {
                    artist.Thumbnail = ImageHelper.ResizeToThumbnail(artist.Thumbnail, Configuration);
                }
                var addArtistResult = DbContext.Artists.Add(artist);
                var inserted = 0;
                inserted = await DbContext.SaveChangesAsync();
                _addedArtistIds.Add(artist.Id);
                if (artist.Id < 1 && addArtistResult.Entity.Id > 0) artist.Id = addArtistResult.Entity.Id;
                if (inserted > 0 && artist.Id > 0)
                {
                    if (artistGenreTables != null && artistGenreTables.Any(x => x.GenreId == null))
                    {
                        foreach (var artistGenreTable in artistGenreTables)
                        {
                            var genreName = artistGenreTable.Genre?.Name?.ToAlphanumericName();
                            if (string.IsNullOrEmpty(genreName)) continue;
                            if (artistGenreTable.Genre.Name.Length > 100)
                            {
                                var originalName = artistGenreTable.Genre.Name;
                                artistGenreTable.Genre.Name = artistGenreTable.Genre.Name.Substring(0, 99);
                                genreName = genreName.Substring(0, 99);
                                Logger.LogWarning($"Genre Name Too long was [{originalName}] truncated to [{artistGenreTable.Genre.Name}]");
                            }
                            var genre = DbContext.Genres.FirstOrDefault(x => x.NormalizedName == genreName);
                            if (genre == null)
                            {
                                genre = new Genre
                                {
                                    Name = artistGenreTable.Genre.Name,
                                    NormalizedName = genreName
                                };
                                DbContext.Genres.Add(genre);
                                await DbContext.SaveChangesAsync();
                            }
                            if (genre != null && genre.Id > 0)
                            {
                                DbContext.ArtistGenres.Add(new ArtistGenre
                                {
                                    ArtistId = artist.Id,
                                    GenreId = genre.Id
                                });
                            }
                        }
                    }

                    if (ArtistImages != null && ArtistImages.Any(x => x.Status == Statuses.New))
                    {
                        foreach (var ArtistImage in ArtistImages)
                            DbContext.Images.Add(new Image
                            {
                                ArtistId = artist.Id,
                                Url = ArtistImage.Url,
                                Signature = ArtistImage.Signature,
                                Bytes = ArtistImage.Bytes
                            });
                        inserted = await DbContext.SaveChangesAsync();
                    }
                    sw.Stop();
                    Logger.LogTrace($"Added New Artist: Elapsed Time [{ sw.ElapsedMilliseconds }], Artist `{ artist }`");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
            }
            return new OperationResult<Artist>
            {
                IsSuccess = artist.Id > 0,
                Data = artist,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public Artist DatabaseQueryForArtistName(string name, string sortName = null)
        {
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
                var searchName = name.NormalizeName();
                var searchSortName = !string.IsNullOrEmpty(sortName) ? sortName.NormalizeName().ToLower() : searchName;
                var specialSearchName = name.ToAlphanumericName();

                var searchNameStart = $"{searchName}|";
                var searchNameIn = $"|{searchName}|";
                var searchNameEnd = $"|{searchName}";

                var specialSearchNameStart = $"{specialSearchName}|";
                var specialSearchNameIn = $"|{specialSearchName}|";
                var specialSearchNameEnd = $"|{specialSearchName}";

                return (from a in DbContext.Artists
                        where a.Name == searchName ||
                              a.Name == specialSearchName ||
                              a.SortName == searchName ||
                              a.SortName == searchSortName ||
                              a.SortName == specialSearchName ||
                              a.AlternateNames.StartsWith(searchNameStart) ||
                              a.AlternateNames.Contains(searchNameIn) ||
                              a.AlternateNames.EndsWith(searchNameEnd) ||
                              a.AlternateNames.StartsWith(specialSearchNameStart) ||
                              a.AlternateNames.Contains(specialSearchNameIn) ||
                              a.AlternateNames.EndsWith(specialSearchNameEnd)
                        select a
                    ).FirstOrDefault();
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

                var artist = DatabaseQueryForArtistName(artistName);
                sw.Stop();
                if (artist == null || !artist.IsValid)
                {
                    Logger.LogTrace("ArtistLookupEngine: Artist Not Found By Name [{0}]", artistName);
                    if (doFindIfNotInDatabase)
                    {
                        OperationResult<Artist> artistSearch = null;

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
                            artist = JsonConvert.DeserializeObject<Artist>(File.ReadAllText(releaseRoadieDataFilename));
                            var addResult = await Add(artist);
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
                            artist = addResult.Data;
                        }
                        else
                        {
                            try
                            {
                                artistSearch = await PerformMetaDataProvidersArtistSearch(metaData);
                                if (artistSearch.IsSuccess)
                                {
                                    artist = artistSearch.Data;
                                    // See if Artist already exist with either Name or Sort Name
                                    var alreadyExists = DatabaseQueryForArtistName(artistSearch.Data.Name, artistSearch.Data.SortNameValue);
                                    if (alreadyExists == null || !alreadyExists.IsValid)
                                    {
                                        var addResult = await Add(artist);
                                        if (!addResult.IsSuccess)
                                        {
                                            sw.Stop();
                                            Logger.LogWarning("Unable To Add Artist For MetaData [{0}]",
                                                metaData.ToString());
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

                if (artist != null && artist.IsValid) CacheManager.Add(cacheKey, artist);
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
                    var iTunesResult = await ITunesArtistSearchEngine.PerformArtistSearch(artistName, 1);
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
                            Thumbnail = i.ArtistThumbnailUrl != null
                                ? WebHelper.BytesForImageUrl(i.ArtistThumbnailUrl)
                                : null,
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
                    var mbResult = await MusicBrainzArtistSearchEngine.PerformArtistSearch(result.Name, 1);
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
                            BeginDate = mb.BeginDate,
                            Name = result.Name ?? mb.ArtistName,
                            SortName = result.SortName ?? mb.ArtistSortName,
                            Thumbnail = mb.ArtistThumbnailUrl != null
                                ? WebHelper.BytesForImageUrl(mb.ArtistThumbnailUrl)
                                : null,
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
                    var lastFmResult = await LastFmArtistSearchEngine.PerformArtistSearch(result.Name, 1);
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
                            result.AlternateNames.AddToDelimitedList(new[] { l.ArtistName });
                        result.CopyTo(new Artist
                        {
                            EndDate = l.EndDate,
                            BioContext = HttpEncoder.HtmlEncode(l.Bio),
                            Profile = HttpEncoder.HtmlEncode(l.Profile),
                            MusicBrainzId = l.MusicBrainzId,
                            BeginDate = l.BeginDate,
                            Name = result.Name ?? l.ArtistName,
                            SortName = result.SortName ?? l.ArtistSortName,
                            Thumbnail = l.ArtistThumbnailUrl != null
                                ? WebHelper.BytesForImageUrl(l.ArtistThumbnailUrl)
                                : null,
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
                    var spotifyResult = await SpotifyArtistSearchEngine.PerformArtistSearch(result.Name, 1);
                    if (spotifyResult.IsSuccess)
                    {
                        var s = spotifyResult.Data.First();
                        if (s.Tags != null) result.Tags = result.Tags.AddToDelimitedList(s.Tags);
                        if (s.Urls != null) result.URLs = result.URLs.AddToDelimitedList(s.Urls);
                        if (s.ImageUrls != null) artistImageUrls.AddRange(s.ImageUrls);
                        if (s.ArtistGenres != null) artistGenres.AddRange(s.ArtistGenres);
                        if (!string.IsNullOrEmpty(s.ArtistName) &&
                            !s.ArtistName.Equals(result.Name, StringComparison.OrdinalIgnoreCase))
                            result.AlternateNames.AddToDelimitedList(new[] { s.ArtistName });
                        result.CopyTo(new Artist
                        {
                            EndDate = s.EndDate,
                            BioContext = s.Bio,
                            Profile = HttpEncoder.HtmlEncode(s.Profile),
                            MusicBrainzId = s.MusicBrainzId,
                            BeginDate = s.BeginDate,
                            Name = result.Name ?? s.ArtistName,
                            SortName = result.SortName ?? s.ArtistSortName,
                            Thumbnail = s.ArtistThumbnailUrl != null
                                ? WebHelper.BytesForImageUrl(s.ArtistThumbnailUrl)
                                : null,
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
                    var discogsResult = await DiscogsArtistSearchEngine.PerformArtistSearch(result.Name, 1);
                    if (discogsResult.IsSuccess)
                    {
                        var d = discogsResult.Data.First();
                        if (d.Urls != null) result.URLs = result.URLs.AddToDelimitedList(d.Urls);
                        if (d.ImageUrls != null) artistImageUrls.AddRange(d.ImageUrls);
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
                            Thumbnail = d.ArtistThumbnailUrl != null
                                ? WebHelper.BytesForImageUrl(d.ArtistThumbnailUrl)
                                : null,
                            ArtistType = result.ArtistType ?? d.ArtistType
                        });
                    }

                    if (discogsResult.Errors != null) resultsExceptions.AddRange(discogsResult.Errors);
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
                    var wikiName = result.Name;
                    // Help get better results for bands with proper nouns (e.g. "Poison" vs "Poison Band")
                    if (!result.ArtistType.Equals("Person", StringComparison.OrdinalIgnoreCase))
                    {
                        wikiName += " band";
                    }
                    var wikipediaResult = await WikipediaArtistSearchEngine.PerformArtistSearch(wikiName, 1);
                    if (wikipediaResult?.Data != null)
                    {
                        if (wikipediaResult.IsSuccess)
                        {
                            var w = wikipediaResult?.Data?.FirstOrDefault();
                            if (w != null)
                            {
                                result.CopyTo(new Artist
                                {
                                    BioContext = HttpEncoder.HtmlEncode(w.Bio ?? string.Empty)
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
                if (artistGenres.Any())
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
                            Genre = genreInfo.existingGenre != null ? genreInfo.existingGenre : new Genre
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

                if (artistImageUrls.Any())
                {
                    var sw2 = Stopwatch.StartNew();
                    var imageBag = new ConcurrentBag<Image>();
                    var i = artistImageUrls.Select(async url =>
                    {
                        imageBag.Add(await WebHelper.GetImageFromUrlAsync(url));
                    });
                    await Task.WhenAll(i);
                    result.Images = imageBag.Where(x => x != null && x.Bytes != null)
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