using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.SearchEngines.MetaData;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using discogs = Roadie.Library.SearchEngines.MetaData.Discogs;

namespace Roadie.Library.Engines
{
    public class LabelLookupEngine : LookupEngineBase, ILabelLookupEngine
    {
        private ILabelSearchEngine DiscogsLabelSearchEngine { get; }

        public LabelLookupEngine(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context,
                    ICacheManager cacheManager, ILogger<LabelLookupEngine> logger, discogs.IDiscogsHelper discogsHelper)
            : base(configuration, httpEncoder, context, cacheManager, logger)
        {
            DiscogsLabelSearchEngine = discogsHelper;
        }

        public async Task<OperationResult<Label>> Add(Label label)
        {
            SimpleContract.Requires<ArgumentNullException>(label != null, "Invalid Label");

            try
            {
                var now = DateTime.UtcNow;
                label.AlternateNames = label.AlternateNames.AddToDelimitedList(new[] { label.Name.ToAlphanumericName() });
                if (label.Thumbnail != null)
                {
                    label.Thumbnail = ImageHelper.ResizeToThumbnail(label.Thumbnail, Configuration);
                }
                if (!label.IsValid)
                {
                    return new OperationResult<Label>
                    {
                        Errors = new Exception[1] { new Exception("Label is Invalid") }
                    };
                }
                DbContext.Labels.Add(label);
                var inserted = 0;
                try
                {
                    inserted = await DbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return new OperationResult<Label>
            {
                IsSuccess = label.Id > 0,
                Data = label
            };
        }

        public async Task<OperationResult<Label>> GetByName(string labelName, bool doFindIfNotInDatabase = false)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                var cacheRegion = new Label { Name = labelName }.CacheRegion;
                var cacheKey = string.Format("urn:Label_by_name:{0}", labelName);
                var resultInCache = CacheManager.Get<Label>(cacheKey, cacheRegion);
                if (resultInCache != null)
                {
                    sw.Stop();
                    return new OperationResult<Label>
                    {
                        IsSuccess = true,
                        OperationTime = sw.ElapsedMilliseconds,
                        Data = resultInCache
                    };
                }

                var searchName = labelName.NormalizeName();
                var specialSearchName = labelName.ToAlphanumericName();
                var label = (from l in DbContext.Labels
                             where l.Name.Contains(searchName) ||
                                   l.SortName.Contains(searchName) ||
                                   l.AlternateNames.Contains(searchName) ||
                                   l.AlternateNames.Contains(specialSearchName)
                             select l
                    ).FirstOrDefault();
                sw.Stop();
                if (label == null || !label.IsValid)
                {
                    Logger.LogTrace("LabelFactory: Label Not Found By Name [{0}]", labelName);
                    if (doFindIfNotInDatabase)
                    {
                        OperationResult<Label> LabelSearch = null;
                        try
                        {
                            LabelSearch = await PerformMetaDataProvidersLabelSearch(labelName);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex);
                        }

                        if (LabelSearch.IsSuccess)
                        {
                            label = LabelSearch.Data;
                            var addResult = await Add(label);
                            if (!addResult.IsSuccess)
                            {
                                sw.Stop();
                                return new OperationResult<Label>
                                {
                                    OperationTime = sw.ElapsedMilliseconds,
                                    Errors = addResult.Errors
                                };
                            }
                        }
                    }
                }
                else
                {
                    CacheManager.Add(cacheKey, label);
                }

                return new OperationResult<Label>
                {
                    IsSuccess = label != null,
                    OperationTime = sw.ElapsedMilliseconds,
                    Data = label
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return new OperationResult<Label>();
        }

        public async Task<OperationResult<Label>> PerformMetaDataProvidersLabelSearch(string LabelName)
        {
            SimpleContract.Requires<ArgumentNullException>(LabelName != null, "Invalid Label Name");

            var sw = new Stopwatch();
            sw.Start();
            var result = new Label
            {
                Name = LabelName.ToTitleCase()
            };
            var resultsExceptions = new List<Exception>();

            if (DiscogsLabelSearchEngine.IsEnabled)
            {
                var discogsResult = await DiscogsLabelSearchEngine.PerformLabelSearch(result.Name, 1);
                if (discogsResult.IsSuccess)
                {
                    var d = discogsResult.Data.First();
                    if (d.Urls != null) result.URLs = result.URLs.AddToDelimitedList(d.Urls);
                    if (d.AlternateNames != null)
                    {
                        result.AlternateNames = result.AlternateNames.AddToDelimitedList(d.AlternateNames);
                    }
                    if (!string.IsNullOrEmpty(d.LabelName) &&
                        !d.LabelName.Equals(result.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        result.AlternateNames.AddToDelimitedList(new[] { d.LabelName });
                    }
                    result.CopyTo(new Label
                    {
                        Profile = HttpEncoder.HtmlEncode(d.Profile),
                        DiscogsId = d.DiscogsId,
                        Name = result.Name ?? d.LabelName.ToTitleCase(),
                        Thumbnail = d.LabelImageUrl != null ? WebHelper.BytesForImageUrl(d.LabelImageUrl) : null
                    });
                }

                if (discogsResult.Errors != null)
                {
                    resultsExceptions.AddRange(discogsResult.Errors);
                }
            }

            sw.Stop();
            return new OperationResult<Label>
            {
                Data = result,
                IsSuccess = result != null,
                Errors = resultsExceptions,
                OperationTime = sw.ElapsedMilliseconds
            };
        }
    }
}