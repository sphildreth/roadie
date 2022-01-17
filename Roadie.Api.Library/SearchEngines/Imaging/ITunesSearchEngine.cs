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
    public class ITunesSearchEngine : ImageSearchEngineBase, IITunesSearchEngine
    {
        public ITunesSearchEngine(IRoadieSettings configuration, ICacheManager cacheManager, ILogger<ITunesSearchEngine> logger, string requestIp = null, string referrer = null)
            : base(configuration, logger, "http://itunes.apple.com", requestIp, referrer)
        {
            CacheManager = cacheManager;
        }

        private ICacheManager CacheManager { get; }

        public override bool IsEnabled => Configuration.Integrations.ITunesProviderEnabled;

        public async Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearchAsync(string query, int resultsCount)
        {
            ArtistSearchResult data = null;

            try
            {
                var request = BuildRequest(query, 1, "musicArtist");
                var response = await _client.ExecuteAsync<ITunesSearchResult>(request).ConfigureAwait(false);
                if (response.ResponseStatus == ResponseStatus.Error)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                        throw new AuthenticationException("Unauthorized");
                    throw new Exception(string.Format("Request Error Message: {0}. Content: {1}.",
                        response.ErrorMessage, response.Content));
                }

                var responseData = response?.Data?.results?.FirstOrDefault();
                if (responseData != null)
                {
                    var urls = new List<string>();
                    if (!string.IsNullOrEmpty(responseData.artistLinkUrl)) urls.Add(responseData.artistLinkUrl);
                    if (!string.IsNullOrEmpty(responseData.artistViewUrl)) urls.Add(responseData.artistViewUrl);
                    if (!string.IsNullOrEmpty(responseData.collectionViewUrl)) urls.Add(responseData.collectionViewUrl);
                    data = new ArtistSearchResult
                    {
                        ArtistName = responseData.artistName,
                        iTunesId = responseData.artistId.ToString(),
                        AmgId = responseData.amgArtistId.ToString(),
                        ArtistType = responseData.artistType,
                        ArtistThumbnailUrl = responseData.artworkUrl100,
                        ArtistGenres = new[] { responseData.primaryGenreName },
                        Urls = urls
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return new OperationResult<IEnumerable<ArtistSearchResult>>
            {
                IsSuccess = data != null,
                Data = new[] { data }
            };
        }

        public override RestRequest BuildRequest(string query, int resultsCount)
        {
            return BuildRequest(query, resultsCount, "Release");
        }

#pragma warning disable CS1998

        public override async Task<IEnumerable<ImageSearchResult>> PerformImageSearchAsync(string query, int resultsCount)
        {
            var request = BuildRequest(query, resultsCount);
            ImageSearchResult[] result = null;
            try
            {
                var response = await _client.ExecuteAsync<ITunesSearchResult>(request);
                if (response.ResponseStatus == ResponseStatus.Error)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                        throw new AuthenticationException("Unauthorized");
                    throw new Exception(string.Format("Request Error Message: {0}. Content: {1}.",
                        response.ErrorMessage, response.Content));
                }

                if (response.Data.results == null) return new ImageSearchResult[0];
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
                Logger.LogError(ex.Serialize());
            }

            return result;
        }

        public async Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName, string query, int resultsCount)
        {
            var request = BuildRequest(query, 1, "album");
            var response = await _client.ExecuteAsync<ITunesSearchResult>(request).ConfigureAwait(false);
            if (response.ResponseStatus == ResponseStatus.Error)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new AuthenticationException("Unauthorized");
                throw new Exception(string.Format("Request Error Message: {0}. Content: {1}.", response.ErrorMessage,
                    response.Content));
            }

            ReleaseSearchResult data = null;
            try
            {
                var responseData = response?.Data?.results?.FirstOrDefault();
                if (responseData != null)
                {
                    var urls = new List<string>();
                    if (!string.IsNullOrEmpty(responseData.artistLinkUrl)) urls.Add(responseData.artistLinkUrl);
                    if (!string.IsNullOrEmpty(responseData.artistViewUrl)) urls.Add(responseData.artistViewUrl);
                    if (!string.IsNullOrEmpty(responseData.collectionViewUrl)) urls.Add(responseData.collectionViewUrl);
                    data = new ReleaseSearchResult
                    {
                        ReleaseTitle = responseData.collectionName,
                        iTunesId = responseData.artistId.ToString(),
                        AmgId = responseData.amgArtistId.ToString(),
                        ReleaseType = responseData.collectionType,
                        ReleaseThumbnailUrl = responseData.artworkUrl100,
                        ReleaseGenres = new[] { responseData.primaryGenreName },
                        Urls = urls
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return new OperationResult<IEnumerable<ReleaseSearchResult>>
            {
                IsSuccess = data != null,
                Data = new[] { data }
            };
        }

        private RestRequest BuildRequest(string query, int resultsCount, string entityType)
        {
            var request = new RestRequest
            {
                Resource = "search",
                Method = Method.Get,
                RequestFormat = DataFormat.Json
            };

            if (resultsCount > 0)
            {
                request.AddParameter("limit", resultsCount, ParameterType.GetOrPost);
            }
            request.AddParameter("entity", entityType, ParameterType.GetOrPost);
            request.AddParameter("country", "us", ParameterType.GetOrPost);
            request.AddParameter("term", string.Format("'{0}'", query.Trim()), ParameterType.GetOrPost);
            return request;
        }
    }
}