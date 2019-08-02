using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
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

            Logger.LogInformation("User `{0}` deleted Genre `{1}]`", user, genre);
            CacheManager.ClearRegion(genre.CacheRegion);
            sw.Stop();
            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }



        private Task<OperationResult<Genre>> GenreByIdAction(Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();

            var genre = DbContext.Genres.FirstOrDefault(x => x.RoadieId == id);
            if (genre == null)
            {
                return Task.FromResult(new OperationResult<Genre>(true, string.Format("Genre Not Found [{0}]", id)));
            }
            var result = genre.Adapt<Genre>();
            result.AlternateNames = genre.AlternateNames;
            result.Tags = genre.Tags;
            result.Thumbnail = MakeLabelThumbnailImage(genre.RoadieId);
            result.MediumThumbnail = MakeThumbnailImage(id, "genre", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height);
            if (includes != null && includes.Any())
            {
                if (includes.Contains("stats"))
                {
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
                }
            }
            sw.Stop();
            return Task.FromResult(new OperationResult<Genre>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        public async Task<OperationResult<Image>> UploadGenreImage(User user, Guid id, IFormFile file)
        {
            var bytes = new byte[0];
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                bytes = ms.ToArray();
            }

            return await SaveImageBytes(user, id, bytes);
        }

        public async Task<OperationResult<Image>> SetGenreImageByUrl(User user, Guid id, string imageUrl)
        {
            return await SaveImageBytes(user, id, WebHelper.BytesForImageUrl(imageUrl));
        }


        private async Task<OperationResult<Image>> SaveImageBytes(User user, Guid id, byte[] imageBytes)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var genre = DbContext.Genres.FirstOrDefault(x => x.RoadieId == id);
            if (genre == null) return new OperationResult<Image>(true, string.Format("Genre Not Found [{0}]", id));
            try
            {
                var now = DateTime.UtcNow;
                genre.Thumbnail = imageBytes;
                if (genre.Thumbnail != null)
                {
                    // Save unaltered label image 
                    File.WriteAllBytes(genre.PathToImage(Configuration), ImageHelper.ConvertToJpegFormat(imageBytes));
                    genre.Thumbnail = ImageHelper.ResizeToThumbnail(genre.Thumbnail, Configuration);
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

            return new OperationResult<Image>
            {
                IsSuccess = !errors.Any(),
                Data = MakeThumbnailImage(id, "genre", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height, true),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }


        public Task<Library.Models.Pagination.PagedResult<GenreList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false)
        {
            var sw = new Stopwatch();
            sw.Start();

            if (!string.IsNullOrEmpty(request.Sort))
            {
                request.Sort = request.Sort.Replace("createdDate", "createdDateTime");
                request.Sort = request.Sort.Replace("lastUpdated", "lastUpdatedDateTime");
            }

            var result = from g in DbContext.Genres
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
                             Thumbnail = MakeGenreThumbnailImage(g.RoadieId)
                         };

            GenreList[] rows;
            var rowCount = result.Count();
            if (doRandomize ?? false)
            {
                var randomLimit = roadieUser?.RandomReleaseLimit ?? 100;
                request.Limit = request.LimitValue > randomLimit ? randomLimit : request.LimitValue;
                rows = result.OrderBy(x => x.RandomSortId).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
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
                TotalCount = rowCount,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            });
        }
    }
}