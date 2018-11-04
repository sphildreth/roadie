﻿using Roadie.Library.Caching;
using MySql.Data.MySqlClient;
using Roadie.Library.Extensions;
using Roadie.Library.Utility;
using Roadie.Library.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Roadie.Library.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Roadie.Library.Encoding;

namespace Roadie.Library.Factories
{
    public sealed class LabelFactory : FactoryBase
    {
        public LabelFactory(IConfiguration configuration, IHttpEncoder httpEncoder, IRoadieDbContext context, ICacheManager cacheManager, ILogger logger) : base(configuration, context, cacheManager, logger, httpEncoder)
        {
        }

        public async Task<FactoryResult<Label>> Add(Label label)
        {
            SimpleContract.Requires<ArgumentNullException>(label != null, "Invalid Label");

            try
            {
                var now = DateTime.UtcNow;
                label.AlternateNames = label.AlternateNames.AddToDelimitedList(new string[] { label.Name.ToAlphanumericName() });
                if (!label.IsValid)
                {
                    return new FactoryResult<Label>
                    {
                        Errors = new List<string> { "Label is Invalid" }
                    };
                }
                this.DbContext.Labels.Add(label);
                int inserted = 0;
                try
                {
                    inserted = await this.DbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    this.Logger.Error(ex);
                }
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex);
            }
            return new FactoryResult<Label>
            {
                IsSuccess = label.Id > 0,
                Data = label
            };
        }

        public async Task<FactoryResult<Label>> GetByName(string LabelName, bool doFindIfNotInDatabase = false)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                var cacheRegion = (new Label { Name = LabelName }).CacheRegion;
                var cacheKey = string.Format("urn:Label_by_name:{0}", LabelName);
                var resultInCache = this.CacheManager.Get<Label>(cacheKey, cacheRegion);
                if (resultInCache != null)
                {
                    sw.Stop();
                    return new FactoryResult<Label>
                    {
                        IsSuccess = true,
                        OperationTime = sw.ElapsedMilliseconds,
                        Data = resultInCache
                    };
                }
                var getParams = new List<object>();
                var searchName = LabelName.NormalizeName().ToLower();
                getParams.Add(new MySqlParameter("@isName", searchName));
                getParams.Add(new MySqlParameter("@startAlt", string.Format("{0}|%", searchName)));
                getParams.Add(new MySqlParameter("@inAlt", string.Format("%|{0}|%", searchName)));
                getParams.Add(new MySqlParameter("@endAlt", string.Format("%|{0}", searchName)));
                var Label = this.DbContext.Labels.FromSql(@"SELECT *
                FROM `Label`
                WHERE LCASE(name) = @isName
                OR LCASE(sortName) = @isName
                OR LCASE(alternatenames) = @isName
                OR alternatenames like @startAlt
                OR alternatenames like @inAlt
                OR alternatenames like @endAlt
                LIMIT 1;", getParams.ToArray()).FirstOrDefault();
                sw.Stop();
                if (Label == null || !Label.IsValid)
                {
                    this._logger.Info("LabelFactory: Label Not Found By Name [{0}]", LabelName);
                    if (doFindIfNotInDatabase)
                    {
                        OperationResult<Label> LabelSearch = null;
                        try
                        {
                            LabelSearch = await this.PerformMetaDataProvidersLabelSearch(LabelName);
                        }
                        catch (Exception ex)
                        {
                            this.Logger.Error(ex);
                        }
                        if (LabelSearch.IsSuccess)
                        {
                            Label = LabelSearch.Data;
                            var addResult = await this.Add(Label);
                            if (!addResult.IsSuccess)
                            {
                                sw.Stop();
                                return new FactoryResult<Label>
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
                    this.CacheManager.Add(cacheKey, Label);
                }
                return new FactoryResult<Label>
                {
                    IsSuccess = Label != null,
                    OperationTime = sw.ElapsedMilliseconds,
                    Data = Label
                };
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex);
            }
            return new FactoryResult<Label>();
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

            if (this.DiscogsLabelSearchEngine.IsEnabled)
            {
                var discogsResult = await this.DiscogsLabelSearchEngine.PerformLabelSearch(result.Name, 1);
                if (discogsResult.IsSuccess)
                {
                    var d = discogsResult.Data.First();
                    if (d.Urls != null)
                    {
                        result.URLs = result.URLs.AddToDelimitedList(d.Urls);
                    }
                    if (d.AlternateNames != null)
                    {
                        result.AlternateNames = result.AlternateNames.AddToDelimitedList(d.AlternateNames);
                    }
                    if (!string.IsNullOrEmpty(d.LabelName) && !d.LabelName.Equals(result.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        result.AlternateNames.AddToDelimitedList(new string[] { d.LabelName });
                    }
                    result.CopyTo(new Label
                    {
                        Profile = this.HttpEncoder.HtmlEncode(d.Profile),
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