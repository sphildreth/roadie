using Microsoft.Extensions.Logging;
using RestSharp;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.MetaData.Discogs
{
    public class DiscogsHelper : MetaDataProviderBase, IDiscogsHelper
    {
        public override bool IsEnabled => Configuration.Integrations.DiscogsProviderEnabled;

        public DiscogsHelper(IRoadieSettings configuration, ICacheManager cacheManager, ILogger<DiscogsHelper> logger) : base(
                    configuration, cacheManager, logger)
        {
            _apiKey = configuration.Integrations.ApiKeys.FirstOrDefault(x => x.ApiName == "DiscogsConsumerKey") ?? new ApiKey();
        }

        public async Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearch(string query, int resultsCount)
        {
            ArtistSearchResult data = null;
            try
            {
                var cacheKey = $"uri:discogs:artistsearch:{ query.ToAlphanumericName() }";
                data = await CacheManager.GetAsync<ArtistSearchResult>(cacheKey, async () =>
                {
                    Logger.LogTrace("DiscogsHelper:PerformArtistSearch:{0}", query);
                    var request = BuildSearchRequest(query, 1, "artist");

                    var client = new RestClient("https://api.discogs.com/database")
                    {
                        UserAgent = WebHelper.UserAgent
                    };

                    var response = await client.ExecuteTaskAsync<DiscogsResult>(request);

                    if (response.ResponseStatus == ResponseStatus.Error)
                    {
                        if (response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            throw new AuthenticationException("Unauthorized");
                        }
                        throw new Exception(string.Format("Request Error Message: {0}. Content: {1}.", response.ErrorMessage, response.Content));
                    }

                    var responseData = response.Data.results != null && response.Data.results.Any()
                        ? response.Data.results.First()
                        : null;
                    if (responseData != null)
                    {
                        request = BuildArtistRequest(responseData.id);
                        var c2 = new RestClient("https://api.discogs.com/")
                        {
                            UserAgent = WebHelper.UserAgent
                        };
                        var artistResponse = await c2.ExecuteTaskAsync<DiscogArtistResponse>(request);
                        var artist = artistResponse.Data;
                        if (artist != null)
                        {
                            var urls = new List<string>();
                            var images = new List<string>();
                            var alternateNames = new List<string>();
                            string artistThumbnailUrl = null;
                            urls.Add(artist.uri);
                            if (artist.urls != null) urls.AddRange(artist.urls);
                            if (artist.images != null)
                            {
                                images.AddRange(artist.images.Where(x => x.type != "primary").Select(x => x.uri));
                                var primaryImage = artist.images.FirstOrDefault(x => x.type == "primary");
                                if (primaryImage != null) artistThumbnailUrl = primaryImage.uri;
                                if (string.IsNullOrEmpty(artistThumbnailUrl))
                                {
                                    artistThumbnailUrl = artist.images.First(x => !string.IsNullOrEmpty(x.uri)).uri;
                                }
                            }
                            if (artist.namevariations != null)
                            {
                                alternateNames.AddRange(artist.namevariations.Distinct());
                            }
                            return new ArtistSearchResult
                            {
                                ArtistName = artist.name,
                                DiscogsId = artist.id.ToString(),
                                ArtistType = responseData.type,
                                Profile = artist.profile,
                                AlternateNames = alternateNames,
                                ArtistThumbnailUrl = artistThumbnailUrl,
                                Urls = urls,
                                ImageUrls = images
                            };
                        }
                    }
                    return null;
                }, "uri:metadata");
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

        public async Task<OperationResult<IEnumerable<LabelSearchResult>>> PerformLabelSearch(string labelName, int resultsCount)
        {
            LabelSearchResult data = null;
            try
            {
                var cacheKey = $"uri:discogs:labelsearch:{ labelName.ToAlphanumericName() }";
                data = await CacheManager.GetAsync<LabelSearchResult>(cacheKey, async () =>
                {
                    var request = BuildSearchRequest(labelName, 1, "label");

                    var client = new RestClient("https://api.discogs.com/database");
                    client.UserAgent = WebHelper.UserAgent;

                    var response = await client.ExecuteTaskAsync<DiscogsResult>(request);

                    if (response.ResponseStatus == ResponseStatus.Error)
                    {
                        if (response.StatusCode == HttpStatusCode.Unauthorized)
                            throw new AuthenticationException("Unauthorized");
                        throw new Exception(string.Format("Request Error Message: {0}. Content: {1}.",
                            response.ErrorMessage, response.Content));
                    }

                    var responseData = response.Data.results != null && response.Data.results.Any()
                        ? response.Data.results.First()
                        : null;
                    if (responseData != null)
                    {
                        request = BuildLabelRequest(responseData.id);
                        var c2 = new RestClient("https://api.discogs.com/");
                        c2.UserAgent = WebHelper.UserAgent;
                        var labelResponse = await c2.ExecuteTaskAsync<DiscogsLabelResult>(request);
                        var label = labelResponse.Data;
                        if (label != null)
                        {
                            var urls = new List<string>();
                            var images = new List<string>();
                            var alternateNames = new List<string>();
                            string labelThumbnailUrl = null;
                            urls.Add(label.uri);
                            if (label.urls != null) urls.AddRange(label.urls);
                            if (label.images != null)
                            {
                                images.AddRange(label.images.Where(x => x.type != "primary").Select(x => x.uri));
                                var primaryImage = label.images.FirstOrDefault(x => x.type == "primary");
                                if (primaryImage != null) labelThumbnailUrl = primaryImage.uri;
                                if (string.IsNullOrEmpty(labelThumbnailUrl))
                                    labelThumbnailUrl = label.images.First(x => !string.IsNullOrEmpty(x.uri)).uri;
                            }

                            data = new LabelSearchResult
                            {
                                LabelName = label.name,
                                DiscogsId = label.id.ToString(),
                                Profile = label.profile,
                                AlternateNames = alternateNames,
                                LabelImageUrl = labelThumbnailUrl,
                                Urls = urls,
                                ImageUrls = images
                            };
                        }
                    }
                    return null;
                }, "uri:metadata");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return new OperationResult<IEnumerable<LabelSearchResult>>
            {
                IsSuccess = data != null,
                Data = new[] { data }
            };
        }

        public async Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName, string query, int resultsCount)
        {
            ReleaseSearchResult data = null;
            try
            {
                var cacheKey = $"uri:discogs:releasesearch:{ artistName.ToAlphanumericName() }:{ query.ToAlphanumericName() }";
                data = await CacheManager.GetAsync<ReleaseSearchResult>(cacheKey, async () =>
                {
                    var request = BuildSearchRequest(query, 10, "release", artistName);

                    var client = new RestClient("https://api.discogs.com/database")
                    {
                        UserAgent = WebHelper.UserAgent,
                        ReadWriteTimeout = SafeParser.ToNumber<int>(Configuration.Integrations.DiscogsReadWriteTimeout),
                        Timeout = SafeParser.ToNumber<int>(Configuration.Integrations.DiscogsTimeout)
                    };

                    var response = await client.ExecuteTaskAsync<DiscogsReleaseSearchResult>(request);
                    if (response?.ResponseStatus == null || response.ResponseStatus == ResponseStatus.Error)
                    {
                        if (response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            throw new AuthenticationException("Unauthorized");
                        }
                        throw new Exception($"Request Error Message: {response?.ErrorMessage}. Content: {response?.Content}.");
                    }

                    var responseData = response?.Data?.results?.OrderBy(x => x.year).FirstOrDefault();
                    if (responseData?.id != null)
                    {
                        request = BuildReleaseRequest(responseData.id);
                        var c2 = new RestClient("https://api.discogs.com/")
                        {
                            UserAgent = WebHelper.UserAgent
                        };
                        var releaseResult = await c2.ExecuteTaskAsync<DiscogReleaseDetail>(request);
                        var release = releaseResult?.Data;
                        if (release != null)
                        {
                            var urls = new List<string>();
                            var images = new List<string>();
                            string releaseThumbnailUrl = null;
                            urls.Add(release.uri);
                            if (release.images != null)
                            {
                                images.AddRange(release.images.Where(x => x.type != "primary").Select(x => x.uri));
                                var primaryImage = release.images.FirstOrDefault(x => x.type == "primary");
                                if (primaryImage != null) releaseThumbnailUrl = primaryImage.uri;
                                if (string.IsNullOrEmpty(releaseThumbnailUrl))
                                    releaseThumbnailUrl = release.images.First(x => !string.IsNullOrEmpty(x.uri)).uri;
                            }

                            data = new ReleaseSearchResult
                            {
                                DiscogsId = release.id.ToString(),
                                ReleaseType = responseData.type,
                                ReleaseDate = SafeParser.ToDateTime(release.released),
                                Profile = release.notes,
                                ReleaseThumbnailUrl = releaseThumbnailUrl,
                                Urls = urls,
                                ImageUrls = images
                            };
                            if (release.genres != null) data.ReleaseGenres = release.genres.ToList();
                            if (release.labels != null)
                                data.ReleaseLabel = release.labels.Select(x => new ReleaseLabelSearchResult
                                {
                                    CatalogNumber = x.catno,
                                    Label = new LabelSearchResult
                                    {
                                        LabelName = x.name,
                                        DiscogsId = x.id.ToString()
                                    }
                                }).ToList();
                            if (release.tracklist != null)
                            {
                                var releaseMediaCount = 1;
                                var releaseMedias = new List<ReleaseMediaSearchResult>();
                                for (short? i = 1; i <= releaseMediaCount; i++)
                                {
                                    var releaseTracks = new List<TrackSearchResult>();
                                    short? looper = 0;
                                    foreach (var dTrack in release.tracklist.OrderBy(x => x.position))
                                    {
                                        looper++;
                                        releaseTracks.Add(new TrackSearchResult
                                        {
                                            TrackNumber = looper,
                                            Title = dTrack.title,
                                            Duration = dTrack.duration.ToTrackDuration(),
                                            TrackType = dTrack.type_
                                        });
                                    }

                                    releaseMedias.Add(new ReleaseMediaSearchResult
                                    {
                                        ReleaseMediaNumber = i,
                                        TrackCount = (short)releaseTracks.Count(),
                                        Tracks = releaseTracks
                                    });
                                }

                                data.ReleaseMedia = releaseMedias;
                            }

                            if (release.identifiers != null)
                            {
                                var barcode = release.identifiers.FirstOrDefault(x => (x.type ?? string.Empty) == "Barcode");
                                if (barcode?.value != null)
                                {
                                    data.Tags = new[] { "barcode:" + barcode.value };
                                }
                            }
                        }
                    }
                    return null;
                }, "uri:metadata");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"DiscogsHelper: Error in PerformReleaseSearch artistname [{ artistName }], query [{ query }], resultsCount [{ resultsCount }]");
            }

            return new OperationResult<IEnumerable<ReleaseSearchResult>>
            {
                IsSuccess = data != null,
                Data = new[] { data }
            };
        }

        private RestRequest BuildArtistRequest(int? artistId)
        {
            var request = new RestRequest
            {
                Resource = "artists/{id}",
                Method = Method.GET,
                RequestFormat = DataFormat.Json
            };
            request.AddUrlSegment("id", artistId.ToString());
            request.AddParameter(new Parameter
            {
                Name = "key",
                Value = ApiKey.Key,
                Type = ParameterType.GetOrPost
            });
            request.AddParameter(new Parameter
            {
                Name = "secret",
                Value = ApiKey.KeySecret,
                Type = ParameterType.GetOrPost
            });

            return request;
        }

        private RestRequest BuildLabelRequest(int? artistId)
        {
            var request = new RestRequest
            {
                Resource = "labels/{id}",
                Method = Method.GET,
                RequestFormat = DataFormat.Json
            };
            request.AddUrlSegment("id", artistId.ToString());
            request.AddParameter(new Parameter
            {
                Name = "key",
                Value = ApiKey.Key,
                Type = ParameterType.GetOrPost
            });
            request.AddParameter(new Parameter
            {
                Name = "secret",
                Value = ApiKey.KeySecret,
                Type = ParameterType.GetOrPost
            });

            return request;
        }

        private RestRequest BuildReleaseRequest(int? releaseId)
        {
            var request = new RestRequest
            {
                Resource = "releases/{id}",
                Method = Method.GET,
                RequestFormat = DataFormat.Json
            };
            request.AddUrlSegment("id", releaseId.ToString());
            request.AddParameter(new Parameter
            {
                Name = "key",
                Value = ApiKey.Key,
                Type = ParameterType.GetOrPost
            });
            request.AddParameter(new Parameter
            {
                Name = "secret",
                Value = ApiKey.KeySecret,
                Type = ParameterType.GetOrPost
            });

            return request;
        }

        private RestRequest BuildSearchRequest(string query, int resultsCount, string entityType, string artist = null)
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
                    Name = "page",
                    Value = 1,
                    Type = ParameterType.GetOrPost
                });
                request.AddParameter(new Parameter
                {
                    Name = "per_page",
                    Value = resultsCount,
                    Type = ParameterType.GetOrPost
                });
            }

            request.AddParameter(new Parameter
            {
                Name = "type",
                Value = entityType,
                Type = ParameterType.GetOrPost
            });
            request.AddParameter(new Parameter
            {
                Name = "q",
                Value = string.Format("'{0}'", query.Trim()),
                Type = ParameterType.GetOrPost
            });
            if (!string.IsNullOrEmpty(artist))
                request.AddParameter(new Parameter
                {
                    Name = "artist",
                    Value = string.Format("'{0}'", artist.Trim()),
                    Type = ParameterType.GetOrPost
                });
            request.AddParameter(new Parameter
            {
                Name = "key",
                Value = ApiKey.Key,
                Type = ParameterType.GetOrPost
            });
            request.AddParameter(new Parameter
            {
                Name = "secret",
                Value = ApiKey.KeySecret,
                Type = ParameterType.GetOrPost
            });

            return request;
        }
    }
}