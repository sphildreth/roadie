using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net.Http;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class GenreService : HttpFactoryServiceBase<GenreService>, IGenreService
    {
        public GenreService(IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            ILogger<GenreService> logger,
            IHttpClientFactory httpClientFactory)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext, httpClientFactory)
        {
        }

        private Task<OperationResult<Genre>> GenreByIdAction(Guid id, IEnumerable<string> includes = null)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();

            var genre = DbContext.Genres.FirstOrDefault(x => x.RoadieId == id);
            if (genre == null)
            {
                return Task.FromResult(new OperationResult<Genre>(true, $"Genre Not Found [{id}]"));
            }
            tsw.Stop();
            timings.Add("getGenre", tsw.ElapsedMilliseconds);

            tsw.Restart();
            var result = genre.Adapt<Genre>();
            result.AlternateNames = genre.AlternateNames;
            result.Tags = genre.Tags;
            result.Thumbnail = ImageHelper.MakeGenreThumbnailImage(Configuration, HttpContext, genre.RoadieId);
            result.MediumThumbnail = ImageHelper.MakeThumbnailImage(Configuration, HttpContext, id, "genre", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height);
            tsw.Stop();
            timings.Add("adapt", tsw.ElapsedMilliseconds);
            if (includes != null && includes.Any())
            {
                if (includes.Contains("stats"))
                {
                    tsw.Restart();
                    var releaseCount = (from rg in DbContext.ReleaseGenres
                                        where rg.GenreId == genre.Id
                                        select rg.Id).Count();
                    var artistCount = (from rg in DbContext.ArtistGenres
                                       where rg.GenreId == genre.Id
                                       select rg.Id).Count();
                    result.Statistics = new Library.Models.Statistics.ReleaseGroupingStatistics
                    {
                        ArtistCount = artistCount,
                        ReleaseCount = releaseCount
                    };
                    tsw.Stop();
                    timings.Add("stats", tsw.ElapsedMilliseconds);
                }
            }
            sw.Stop();
            Logger.LogInformation($"ByIdAction: Genre `{ genre }`: includes [{includes.ToCSV()}], timings: [{ timings.ToTimings() }]");
            return Task.FromResult(new OperationResult<Genre>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        private async Task<OperationResult<Library.Models.Image>> SaveImageBytes(Library.Models.Users.User user, Guid id, byte[] imageBytes)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var genre = await DbContext.Genres.FirstOrDefaultAsync(x => x.RoadieId == id).ConfigureAwait(false);
            if (genre == null)
            {
                return new OperationResult<Library.Models.Image>(true, $"Genre Not Found [{id}]");
            }
            try
            {
                var now = DateTime.UtcNow;
                if (imageBytes != null)
                {
                    // Save unaltered genre image
                    File.WriteAllBytes(genre.PathToImage(Configuration, true), ImageHelper.ConvertToJpegFormat(imageBytes));
                }
                genre.LastUpdated = now;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
                CacheManager.ClearRegion(genre.CacheRegion);
                Logger.LogInformation($"UploadGenreImage `{genre}` By User `{user}`");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                errors.Add(ex);
            }

            sw.Stop();

            return new OperationResult<Library.Models.Image>
            {
                IsSuccess = !errors.Any(),
                Data = ImageHelper.MakeThumbnailImage(Configuration, HttpContext, id, "genre", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height, true),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<Genre>> ByIdAsync(Library.Models.Users.User roadieUser, Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = $"urn:genre_by_id_operation:{id}:{(includes == null ? "0" : string.Join("|", includes))}";
            var result = await CacheManager.GetAsync(cacheKey, async () =>
            {
                return await GenreByIdAction(id, includes).ConfigureAwait(false);
            }, data.Genre.CacheRegionUrn(id)).ConfigureAwait(false);
            sw.Stop();
            return new OperationResult<Genre>(result.Messages)
            {
                Data = result?.Data,
                IsNotFoundResult = result?.IsNotFoundResult ?? false,
                Errors = result?.Errors,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> DeleteAsync(Library.Identity.User user, Guid id)
        {
            var sw = new Stopwatch();
            sw.Start();
            var genre = await DbContext.Genres.FirstOrDefaultAsync(x => x.RoadieId == id).ConfigureAwait(false);
            if (genre == null)
            {
                return new OperationResult<bool>(true, $"Genre Not Found [{id}]");
            }

            DbContext.Genres.Remove(genre);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            var genreImageFilename = genre.PathToImage(Configuration);
            if (File.Exists(genreImageFilename))
            {
                File.Delete(genreImageFilename);
            }

            Logger.LogWarning("User `{0}` deleted Genre `{1}]`", user, genre);
            CacheManager.ClearRegion(genre.CacheRegion);
            sw.Stop();
            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<Library.Models.Pagination.PagedResult<GenreList>> ListAsync(Library.Models.Users.User roadieUser, PagedRequest request, bool? doRandomize = false)
        {
            var sw = new Stopwatch();
            sw.Start();

            int? rowCount = null;

            if (!string.IsNullOrEmpty(request.Sort))
            {
                request.Sort = request.Sort.Replace("createdDate", "createdDateTime");
                request.Sort = request.Sort.Replace("lastUpdated", "lastUpdatedDateTime");
            }

            int[] randomGenreIds = null;
            SortedDictionary<int, int> randomGenreData = null;
            if (doRandomize ?? false)
            {
                var randomLimit = request.Limit ?? roadieUser?.RandomReleaseLimit ?? request.LimitValue;
                randomGenreData = await DbContext.RandomGenreIdsAsync(roadieUser?.Id ?? -1, randomLimit, request.FilterFavoriteOnly, request.FilterRatedOnly).ConfigureAwait(false);
                randomGenreIds = randomGenreData.Select(x => x.Value).ToArray();
                rowCount = DbContext.Genres.Count();
            }

            var normalizedFilterValue = !string.IsNullOrEmpty(request.FilterValue)
                ? request.FilterValue.ToAlphanumericName()
                : null;

            var result = from g in DbContext.Genres
                         where randomGenreIds == null || randomGenreIds.Contains(g.Id)
                         let releaseCount = (from rg in DbContext.ReleaseGenres
                                             where rg.GenreId == g.Id
                                             select rg.Id).Count()
                         let artistCount = (from rg in DbContext.ArtistGenres
                                            where rg.GenreId == g.Id
                                            select rg.Id).Count()
                         where string.IsNullOrEmpty(normalizedFilterValue) || (
                                   g.Name.ToLower().Contains(normalizedFilterValue) ||
                                   g.SortName.ToLower().Contains(normalizedFilterValue) ||
                                   g.AlternateNames.ToLower().Contains(normalizedFilterValue)
                               )
                         select new GenreList
                         {
                             DatabaseId = g.Id,
                             Id = g.RoadieId,
                             Genre = new DataToken
                             {
                                 Text = g.Name,
                                 Value = g.RoadieId.ToString()
                             },
                             ReleaseCount = releaseCount,
                             ArtistCount = artistCount,
                             CreatedDate = g.CreatedDate,
                             LastUpdated = g.LastUpdated,
                             Thumbnail = ImageHelper.MakeGenreThumbnailImage(Configuration, HttpContext, g.RoadieId)
                         };
            GenreList[] rows;
            rowCount = rowCount ?? result.Count();
            if (doRandomize ?? false)
            {
                var resultData = result.ToArray();
                rows = (from r in resultData
                        join ra in randomGenreData on r.DatabaseId equals ra.Value
                        orderby ra.Key
                        select r
                       ).ToArray();
            }
            else
            {
                var sortBy = string.IsNullOrEmpty(request.Sort)
                    ? request.OrderValue(new Dictionary<string, string> { { "Genre.Text", "ASC" } })
                    : request.OrderValue();
                rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            }

            sw.Stop();
            return (new Library.Models.Pagination.PagedResult<GenreList>
            {
                TotalCount = rowCount.Value,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            });
        }

        public async Task<OperationResult<Library.Models.Image>> SetGenreImageByUrlAsync(Library.Models.Users.User user, Guid id, string imageUrl) => await SaveImageBytes(user, id, await WebHelper.BytesForImageUrl(HttpClientFactory, imageUrl).ConfigureAwait(false)).ConfigureAwait(false);

        public async Task<OperationResult<bool>> UpdateGenreAsync(Library.Models.Users.User user, Genre model)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var genre = await DbContext.Genres.FirstOrDefaultAsync(x => x.RoadieId == model.Id).ConfigureAwait(false);
            if (genre == null)
            {
                return new OperationResult<bool>(true, $"Genre Not Found [{model.Id}]");
            }
            // If genre is being renamed, see if genre already exists with new model supplied name
            var genreName = genre.SortNameValue;
            var genreModelName = genre.SortNameValue;
            var didChangeName = !genreName.ToAlphanumericName().Equals(genreModelName.ToAlphanumericName(), StringComparison.OrdinalIgnoreCase);
            if (didChangeName)
            {
                var existingGenre = DbContext.Genres.FirstOrDefault(x => x.Name == model.Name || x.SortName == model.SortName);
                if (existingGenre != null)
                {
                    return new OperationResult<bool>($"Genre already exists `{ existingGenre }` with name [{ genreModelName }].");
                }
            }
            try
            {
                var now = DateTime.UtcNow;
                var specialGenreName = model.Name.ToAlphanumericName();
                var alt = new List<string>(model.AlternateNamesList);
                if (!model.AlternateNamesList.Contains(specialGenreName, StringComparer.OrdinalIgnoreCase))
                {
                    alt.Add(specialGenreName);
                }
                genre.AlternateNames = alt.ToDelimitedList();
                genre.Description = model.Description;
                genre.IsLocked = model.IsLocked;
                var oldPathToImage = genre.PathToImage(Configuration);
                genre.Name = model.Name;
                genre.NormalizedName = model.NormalizedName;
                genre.SortName = model.SortName;
                genre.Status = SafeParser.ToEnum<Statuses>(model.Status);
                genre.Tags = model.TagsList.ToDelimitedList();

                if (didChangeName)
                {
                    if (File.Exists(oldPathToImage))
                    {
                        File.Move(oldPathToImage, genre.PathToImage(Configuration));
                    }
                }
                var genreImage = ImageHelper.ImageDataFromUrl(model.NewThumbnailData);
                if (genreImage != null)
                {
                    // Save unaltered genre image
                    File.WriteAllBytes(genre.PathToImage(Configuration), ImageHelper.ConvertToJpegFormat(genreImage));
                }
                genre.LastUpdated = now;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                CacheManager.ClearRegion(genre.CacheRegion);
                Logger.LogInformation($"UpdateGenre `{genre}` By User `{user}`");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                errors.Add(ex);
            }

            sw.Stop();

            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = errors.Count == 0,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<Library.Models.Image>> UploadGenreImageAsync(Library.Models.Users.User user, Guid id, IFormFile file)
        {
            var bytes = new byte[0];
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                bytes = ms.ToArray();
            }
            return await SaveImageBytes(user, id, bytes).ConfigureAwait(false);
        }
    }
}