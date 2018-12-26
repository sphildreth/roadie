using Microsoft.Extensions.Logging;
using RestSharp;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.SearchEngines.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.Imaging
{
    public class ITunesSearchEngine : ImageSearchEngineBase, IArtistSearchEngine, IReleaseSearchEngine, IITunesSearchEngine
    {
        private readonly ICacheManager _cacheManager = null;

        public bool IsEnabled
        {
            get
            {
                return true;
            }
        }

        private ICacheManager CacheManager
        {
            get
            {
                return this._cacheManager;
            }
        }

        public ITunesSearchEngine(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger, string requestIp = null, string referrer = null)
            : base(configuration, logger, "http://itunes.apple.com", requestIp, referrer)
        {
            this._cacheManager = cacheManager;
        }

        public override RestRequest BuildRequest(string query, int resultsCount)
        {
            return this.BuildRequest(query, resultsCount, "Release");
        }

        public async Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearch(string query, int resultsCount)
        {
            ArtistSearchResult data = null;

            try
            {
                var request = this.BuildRequest(query, 1, "musicArtist");
                var response = await _client.ExecuteTaskAsync<ITunesSearchResult>(request);
                if (response.ResponseStatus == ResponseStatus.Error)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new AuthenticationException("Unauthorized");
                    }
                    throw new Exception(string.Format("Request Error Message: {0}. Content: {1}.", response.ErrorMessage, response.Content));
                }
                Result responseData = response.Data.resultCount > 0 && response.Data.results != null ? response.Data.results.First() : null;
                if (responseData != null)
                {
                    var urls = new List<string>();
                    if (!string.IsNullOrEmpty(responseData.artistLinkUrl))
                    {
                        urls.Add(responseData.artistLinkUrl);
                    }
                    if (!string.IsNullOrEmpty(responseData.artistViewUrl))
                    {
                        urls.Add(responseData.artistViewUrl);
                    }
                    if (!string.IsNullOrEmpty(responseData.collectionViewUrl))
                    {
                        urls.Add(responseData.collectionViewUrl);
                    }
                    data = new ArtistSearchResult
                    {
                        ArtistName = responseData.artistName,
                        iTunesId = responseData.artistId.ToString(),
                        AmgId = responseData.amgArtistId.ToString(),
                        ArtistType = responseData.artistType,
                        ArtistThumbnailUrl = responseData.artworkUrl100,
                        ArtistGenres = new string[] { responseData.primaryGenreName },
                        Urls = urls
                    };
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
            }
            return new OperationResult<IEnumerable<ArtistSearchResult>>
            {
                IsSuccess = data != null,
                Data = new ArtistSearchResult[] { data }
            };
        }

        #pragma warning disable CS1998
        public override async Task<IEnumerable<ImageSearchResult>> PerformImageSearch(string query, int resultsCount)
        {
            var request = this.BuildRequest(query, resultsCount);
            ImageSearchResult[] result = null;
            try
            {
                var response = _client.Execute<ITunesSearchResult>(request);
                if (response.ResponseStatus == ResponseStatus.Error)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new AuthenticationException("Unauthorized");
                    }
                    throw new Exception(string.Format("Request Error Message: {0}. Content: {1}.", response.ErrorMessage, response.Content));
                }
                if (response.Data.results == null)
                {
                    return new ImageSearchResult[0];
                }
                result = response.Data.results.Select(x => new ImageSearchResult
                {
                    ArtistId = x.artistId.ToString(),
                    ArtistName = x.artistName,
                    MediaUrl = x.artworkUrl100,
                    Height = "100",
                    Width = "100",
                    Title = x.collectionName
                }).ToArray();
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex.Serialize());
            }
            return result;
        }

        public async Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName, string query, int resultsCount)
        {
            var request = this.BuildRequest(query, 1, "album");
            var response = await _client.ExecuteTaskAsync<ITunesSearchResult>(request);
            if (response.ResponseStatus == ResponseStatus.Error)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new AuthenticationException("Unauthorized");
                }
                throw new Exception(string.Format("Request Error Message: {0}. Content: {1}.", response.ErrorMessage, response.Content));
            }
            ReleaseSearchResult data = null;
            try
            {
                Result responseData = response.Data.results != null && response.Data.results.Any() ? response.Data.results.First() : null;
                if (responseData != null)
                {
                    var urls = new List<string>();
                    if (!string.IsNullOrEmpty(responseData.artistLinkUrl))
                    {
                        urls.Add(responseData.artistLinkUrl);
                    }
                    if (!string.IsNullOrEmpty(responseData.artistViewUrl))
                    {
                        urls.Add(responseData.artistViewUrl);
                    }
                    if (!string.IsNullOrEmpty(responseData.collectionViewUrl))
                    {
                        urls.Add(responseData.collectionViewUrl);
                    }
                    data = new ReleaseSearchResult
                    {
                        ReleaseTitle = responseData.collectionName,
                        iTunesId = responseData.artistId.ToString(),
                        AmgId = responseData.amgArtistId.ToString(),
                        ReleaseType = responseData.collectionType,
                        ReleaseThumbnailUrl = responseData.artworkUrl100,
                        ReleaseGenres = new string[] { responseData.primaryGenreName },
                        Urls = urls
                    };
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
            }
            return new OperationResult<IEnumerable<ReleaseSearchResult>>
            {
                IsSuccess = data != null,
                Data = new ReleaseSearchResult[] { data }
            };
        }

        private RestRequest BuildRequest(string query, int resultsCount, string entityType)
        {
            var request = new RestRequest
            {
                Resource = "search",
                Method = Method.GET,
                RequestFormat = DataFormat.Json
            };

            if (resultsCount > 0)
            {
                request.AddParameter(new Parameter
                {
                    Name = "limit",
                    Value = resultsCount,
                    Type = ParameterType.GetOrPost
                });
            }

            request.AddParameter(new Parameter
            {
                Name = "entity",
                Value = entityType,
                Type = ParameterType.GetOrPost
            });

            request.AddParameter(new Parameter
            {
                Name = "country",
                Value = "us",
                Type = ParameterType.GetOrPost
            });

            request.AddParameter(new Parameter
            {
                Name = "term",
                Value = string.Format("'{0}'", query.Trim()),
                Type = ParameterType.GetOrPost
            });

            return request;
        }
    }
}