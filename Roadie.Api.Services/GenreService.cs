using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Extensions;
using Roadie.Library.Identity;
using Roadie.Library.Imaging;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class GenreService : ServiceBase, IGenreService
    {
        public GenreService(IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            data.IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            ILogger<GenreService> logger)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
        }

        public async Task<OperationResult<Genre>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = string.Format("urn:genre_by_id_operation:{0}:{1}", id, includes == null ? "0" : string.Join("|", includes));
            var result = await CacheManager.GetAsync(cacheKey, async () =>
            {
                return await GenreByIdAction(id, includes);
            }, data.Genre.CacheRegionUrn(id));
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

        public async Task<OperationResult<bool>> Delete(ApplicationUser user, Guid id)
        {
            var sw = new Stopwatch();
            sw.Start();
            var genre = DbContext.Genres.FirstOrDefault(x => x.RoadieId == id);
            if (genre == null) return new OperationResult<bool>(true, string.Format("Genre Not Found [{0}]", id));
            DbContext.Genres.Remove(genre);
            await DbContext.SaveChangesAsync();

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

        public Task<Library.Models.Pagination.PagedResult<GenreList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false)
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
            if (doRandomize ?? false)
            {
                var randomLimit = request.Limit ?? roadieUser?.RandomReleaseLimit ?? request.LimitValue;
                // This is MySQL specific but I can't figure out how else to get random without throwing EF local evaluate warnings.
                var sql = @"select g.id
                            FROM `genre` g
                            order BY RIGHT( HEX( (1<<24) * (1+RAND()) ), 6)
                            LIMIT 0, {0}";
                randomGenreIds = (from l in DbContext.Genres.FromSqlRaw(sql, randomLimit)
                                  select l.Id).ToArray();
                rowCount = DbContext.Genres.Count();
            }

            var result = from g in DbContext.Genres
                         where randomGenreIds == null || randomGenreIds.Contains(g.Id)
                         let releaseCount = (from rg in DbContext.ReleaseGenres
                                             where rg.GenreId == g.Id
                                             select rg.Id).Count()
                         let artistCount = (from rg in DbContext.ArtistGenres
                                            where rg.GenreId == g.Id
                                            select rg.Id).Count()
                         where request.FilterValue.Length == 0 || g.Name.Contains(request.FilterValue)
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
                             Thumbnail = MakeGenreThumbnailImage(Configuration, HttpContext, g.RoadieId)
                         };

            GenreList[] rows;
            rowCount = rowCount ?? result.Count();
            if (doRandomize ?? false)
            {
                rows = result.ToArray();
            }
            else
            {
                var sortBy = string.IsNullOrEmpty(request.Sort)
                    ? request.OrderValue(new Dictionary<string, string> { { "Genre.Text", "ASC" } })
                    : request.OrderValue();
                rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            }

            sw.Stop();
            return Task.FromResult(new Library.Models.Pagination.PagedResult<GenreList>
            {
                TotalCount = rowCount.Value,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            });
        }

        public async Task<OperationResult<Library.Models.Image>> SetGenreImageByUrl(User user, Guid id, string imageUrl)
        {
            return await SaveImageBytes(user, id, WebHelper.BytesForImageUrl(imageUrl));
        }

        public async Task<OperationResult<Library.Models.Image>> UploadGenreImage(User user, Guid id, IFormFile file)
        {
            var bytes = new byte[0];
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                bytes = ms.ToArray();
            }

            return await SaveImageBytes(user, id, bytes);
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
                return Task.FromResult(new OperationResult<Genre>(true, string.Format("Genre Not Found [{0}]", id)));
            }
            tsw.Stop();
            timings.Add("getGenre", tsw.ElapsedMilliseconds);

            tsw.Restart();
            var result = genre.Adapt<Genre>();
            result.AlternateNames = genre.AlternateNames;
            result.Tags = genre.Tags;
            result.Thumbnail = MakeLabelThumbnailImage(Configuration, HttpContext, genre.RoadieId);
            result.MediumThumbnail = MakeThumbnailImage(Configuration, HttpContext, id, "genre", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height);
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

        private async Task<OperationResult<Library.Models.Image>> SaveImageBytes(User user, Guid id, byte[] imageBytes)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var genre = DbContext.Genres.FirstOrDefault(x => x.RoadieId == id);
            if (genre == null)
            {
                return new OperationResult<Library.Models.Image>(true, string.Format("Genre Not Found [{0}]", id));
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
                await DbContext.SaveChangesAsync();
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
                Data = MakeThumbnailImage(Configuration, HttpContext, id, "genre", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height, true),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }
    }
}