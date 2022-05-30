using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.Models.Users;
using Roadie.Library.Scrobble;
using Roadie.Library.SearchEngines.MetaData;
using Roadie.Library.SearchEngines.MetaData.LastFm;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace Roadie.Library.MetaData.LastFm
{
    public class LastFmHelper : MetaDataProviderBase, ILastFmHelper
    {
        private const string LastFmErrorCodeXPath = "/lfm/error/@code";

        private const string LastFmErrorXPath = "/lfm/error";

        private const string LastFmStatusOk = "ok";

        private const string LastFmStatusXPath = "/lfm/@status";

        public override bool IsEnabled =>
            Configuration.Integrations.LastFmProviderEnabled &&
            !string.IsNullOrEmpty(Configuration.Integrations.LastFMApiKey) &&
            !string.IsNullOrEmpty(Configuration.Integrations.LastFmApiSecret);

        private IHttpEncoder HttpEncoder { get; }

        private LastAuth LastFmTrackApiAuth { get; }

        private IF.Lastfm.Core.Api.TrackApi LastFmTrackApi { get; }

        private IF.Lastfm.Core.Scrobblers.MemoryScrobbler LastFmScrobbler { get; }

        public int SortOrder => 0;

        public LastFmHelper(
            IRoadieSettings configuration,
            ICacheManager cacheManager,
            ILogger<LastFmHelper> logger,
            IHttpEncoder httpEncoder,
            IHttpClientFactory httpClientFactory)
            : base(configuration, cacheManager, logger, httpClientFactory)
        {
            _apiKey = configuration.Integrations.ApiKeys.FirstOrDefault(x => x.ApiName == "LastFMApiKey") ?? new ApiKey();
            HttpEncoder = httpEncoder;

            LastFmTrackApiAuth = new LastAuth(ApiKey.Key, ApiKey.KeySecret);
            LastFmTrackApi = new IF.Lastfm.Core.Api.TrackApi(LastFmTrackApiAuth, HttpClient);
            LastFmScrobbler = new IF.Lastfm.Core.Scrobblers.MemoryScrobbler(LastFmTrackApiAuth, HttpClient);
        }

        public static void CheckLastFmStatus(XPathNavigator navigator)
        {
            CheckLastFmStatus(navigator, null);
        }

        public static void CheckLastFmStatus(XPathNavigator navigator, WebException webException)
        {
            var node = SelectSingleNode(navigator, LastFmStatusXPath);

            if (node.Value == LastFmStatusOk) return;

            throw new LastFmApiException(string.Format("LastFm status = \"{0}\". Error code = {1}. {2}",
                node.Value,
                SelectSingleNode(navigator, LastFmErrorCodeXPath),
                SelectSingleNode(navigator, LastFmErrorXPath)), webException);
        }

        public static XPathNavigator SelectSingleNode(XPathNavigator navigator, string xpath)
        {
            var node = navigator.SelectSingleNode(xpath);
            if (node == null)
                throw new InvalidOperationException(
                    "Node is null. Cannot select single node. XML response may be mal-formed");
            return node;
        }

        public async Task<OperationResult<string>> GetSessionKeyForUserToken(string token)
        {
            var result = false;
            string sessionKey = null;
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    {"token", token}
                };
                string responseXML = null;
                var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl("auth.getSession", parameters));
                request.Headers.Add("User-Agent", WebHelper.UserAgent);
                var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    responseXML = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                var doc = new XmlDocument();
                doc.LoadXml(responseXML);
                sessionKey = doc.GetElementsByTagName("key")[0].InnerText;
                result = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error in LastFmHelper GetSessionKeyForUserToken: token  [{token}]");
            }
            return new OperationResult<string>
            {
                Data = sessionKey,
                IsSuccess = result
            };
        }

        public async Task<OperationResult<bool>> NowPlaying(User roadieUser, ScrobbleInfo scrobble)
        {
            var result = false;
            string msg = null;
            try
            {
                if (!IsEnabled)
                {
                    return new OperationResult<bool>("LastFM Integation Disabled");
                }
                // User should be cached if not then skip as probably a bad user
                var user = CacheManager.Get<Identity.User>(Identity.User.CacheUrn(roadieUser.UserId));
                if (string.IsNullOrEmpty(user?.LastFMSessionKey))
                {
                    return new OperationResult<bool>("User does not have LastFM Integration setup");
                }
                LastFmTrackApiAuth.LoadSession(new LastUserSession { Token = user.LastFMSessionKey });
                var lastFmScrobble = new IF.Lastfm.Core.Objects.Scrobble(
                    scrobble.ArtistName,
                    scrobble.ReleaseTitle,
                    scrobble.TrackTitle,
                    DateTimeOffset.UtcNow)
                {
                    Duration = scrobble.TrackDuration
                };
                var nowPlayingResponse = await LastFmTrackApi.UpdateNowPlayingAsync(lastFmScrobble).ConfigureAwait(false);
                result = nowPlayingResponse?.Success ?? false;
                Logger.LogTrace($"LastFmHelper: NowPlaying : Success [{ result }] RoadieUser `{roadieUser}` NowPlaying `{scrobble}` LastFMResponse `{ CacheManager.CacheSerializer.Serialize(nowPlayingResponse) }`");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error in LastFmHelper NowPlaying: RoadieUser `{roadieUser}` Scrobble `{scrobble}` Http Msg [{ msg }]");
            }
            return new OperationResult<bool>
            {
                IsSuccess = result,
                Data = result
            };
        }

        public async Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearchAsync(string query, int resultsCount)
        {
            try
            {
                var cacheKey = $"uri:lastfm:artistsearch:{ query.ToAlphanumericName() }";
                var data = await CacheManager.GetAsync<ArtistSearchResult>(cacheKey, async () =>
                {
                    Logger.LogTrace("LastFmHelper:PerformArtistSearch:{0}", query);
                    var auth = new LastAuth(ApiKey.Key, ApiKey.KeySecret);
                    var albumApi = new ArtistApi(auth, HttpClient);
                    var response = await albumApi.GetInfoAsync(query).ConfigureAwait(false);
                    if (!response.Success)
                    {
                        return null;
                    }
                    var lastFmArtist = response.Content;
                    var result = new ArtistSearchResult
                    {
                        ArtistName = lastFmArtist.Name,
                        LastFMId = lastFmArtist.Id,
                        MusicBrainzId = lastFmArtist.Mbid,
                        Bio = lastFmArtist.Bio != null ? lastFmArtist.Bio.Content : null
                    };
                    if (lastFmArtist.Tags != null)
                    {
                        result.Tags = lastFmArtist.Tags.Select(x => x.Name).ToList();
                    }
                    if (lastFmArtist.MainImage != null)
                    {
                        result.ImageUrls = new string[1] { lastFmArtist.MainImage.Largest?.AbsoluteUri };
                    }

                    if (lastFmArtist.Url != null)
                    {
                        result.Urls = new[] { lastFmArtist.Url.ToString() };
                    }
                    return result;
                }, "uri:metadata").ConfigureAwait(false);
                return new OperationResult<IEnumerable<ArtistSearchResult>>
                {
                    IsSuccess = data != null,
                    Data = new[] { data }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
            }

            return new OperationResult<IEnumerable<ArtistSearchResult>>();
        }

        public async Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName, string query, int resultsCount)
        {
            var cacheKey = $"uri:lastfm:releasesearch:{ artistName.ToAlphanumericName() }:{ query.ToAlphanumericName() }";
            var data = await CacheManager.GetAsync<ReleaseSearchResult>(cacheKey, async () =>
            {
                var auth = new LastAuth(ApiKey.Key, ApiKey.KeySecret);
                var releaseApi = new IF.Lastfm.Core.Api.AlbumApi(auth, HttpClient);
                var sendResponse = await releaseApi.GetInfoAsync(artistName, query).ConfigureAwait(false);
                if (!sendResponse.Success)
                {
                    return null;
                }
                var response = sendResponse.Content;

                ReleaseSearchResult result = null;
                if (response != null)
                {
                    var lastFmAlbum = response;
                    result = new ReleaseSearchResult
                    {
                        ReleaseTitle = lastFmAlbum.Name,
                        MusicBrainzId = lastFmAlbum.Mbid
                    };

                    if (lastFmAlbum.Images != null)
                    {
                        result.ImageUrls = new string[1] { lastFmAlbum.Images.Largest.AbsoluteUri };
                    }

                    if (lastFmAlbum.TopTags?.Any() == true)
                    {
                        result.Tags = FilterTags(lastFmAlbum.TopTags.Select(x => x.Name));
                    }
                    else
                    {
                        var tagResult = await releaseApi.GetTopTagsAsync(artistName, query).ConfigureAwait(false);
                        if (tagResult != null)
                        {
                            result.Tags = FilterTags(tagResult.Select(x => x.Name).Distinct().Take(25).ToArray());
                        }
                    }
                    LastTrack[] lastFmTracks = null;
                    try
                    {
                        lastFmTracks = lastFmAlbum.Tracks?.Where(x => x != null).ToArray();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"Error getting LastFM Tracks for [{ artistName }] Query [{ query }]");
                    }
                    if (lastFmTracks?.Any() == true)
                    {
                        var tracks = new List<TrackSearchResult>();
                        foreach (var lastFmTrack in lastFmTracks)
                        {
                            tracks.Add(new TrackSearchResult
                            {
                                TrackNumber = SafeParser.ToNumber<short?>(lastFmTrack.Rank),
                                Title = lastFmTrack.Name,
                                Duration = SafeParser.ToNumber<int?>(lastFmTrack.Duration),
                                Urls = lastFmTrack.Url != null ? new[] { lastFmTrack.Url.AbsoluteUri } : null
                            });
                        }
                        result.ReleaseMedia = new List<ReleaseMediaSearchResult>
                        {
                            new ReleaseMediaSearchResult
                            {
                                ReleaseMediaNumber = 1,
                                Tracks = tracks
                            }
                        };
                    }
                }
                return result;
            }, "uri:metadata").ConfigureAwait(false);
            return new OperationResult<IEnumerable<ReleaseSearchResult>>
            {
                IsSuccess = data != null,
                Data = new[] { data }
            };
        }

        private static IEnumerable<string> _tagsToIgnore = new List<string>
        {
            "albums I listened to",
            "albums i own on vinyl",
            "albums I own",
            "everything",
            "favorite album",
            "me in concert",
            "my favorites",
            "our wedding dance",
            "records i own",
            "reminds me of my childhood",
            "vinyls i own"
        };

        private List<string> FilterTags(IEnumerable<string> tags)
        {
            if (tags.Any() == false)
            {
                return null;
            }
            var result = tags.Where(x => !string.IsNullOrWhiteSpace(x) && x.Length > 1).ToList();
            return result.Except(_tagsToIgnore).ToList();
        }

        public async Task<OperationResult<bool>> Scrobble(User roadieUser, ScrobbleInfo scrobble)
        {
            var result = false;
            try
            {
                if (!IsEnabled)
                {
                    return new OperationResult<bool>("LastFM Integation Disabled");
                }
                // LastFM Rules on scrobbling:
                // * The track must be longer than 30 seconds.
                // * And the track has been played for at least half its duration, or for 4 minutes(whichever occurs earlier.)
                if (scrobble.TrackDuration.TotalSeconds < 30)
                {
                    return new OperationResult<bool>("Track duration or elapsed time does not qualify for LastFM Scrobbling");
                }
                // If less than half of duration then create a NowPlaying
                if (scrobble.ElapsedTimeOfTrackPlayed.TotalMinutes < 4 ||
                    scrobble.ElapsedTimeOfTrackPlayed.TotalSeconds < scrobble.TrackDuration.TotalSeconds / 2)
                {
                    return await NowPlaying(roadieUser, scrobble).ConfigureAwait(false);
                }
                // User should be cached if not then skip as probably a bad user
                var user = CacheManager.Get<Identity.User>(Identity.User.CacheUrn(roadieUser.UserId));
                if (string.IsNullOrEmpty(user?.LastFMSessionKey))
                {
                    return new OperationResult<bool>("User does not have LastFM Integration setup");
                }
                LastFmTrackApiAuth.LoadSession(new LastUserSession { Token = user.LastFMSessionKey });
                var lastFmScrobble = new IF.Lastfm.Core.Objects.Scrobble(
                    scrobble.ArtistName,
                    scrobble.ReleaseTitle,
                    scrobble.TrackTitle,
                    scrobble.TimePlayed)
                {
                    Duration = scrobble.TrackDuration,
                    ChosenByUser = !scrobble.IsRandomizedScrobble
                };
                var scrobbleResponse = await LastFmScrobbler.ScrobbleAsync(lastFmScrobble);
                result = scrobbleResponse?.Success ?? false;
                Logger.LogTrace($"LastFmHelper: Scrobble : Success [{ result }] RoadieUser `{roadieUser}` Scrobble `{scrobble}`");
                result = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error in LastFmHelper Scrobble: RoadieUser `{roadieUser}` Scrobble `{scrobble}`");
            }

            return new OperationResult<bool>
            {
                IsSuccess = result,
                Data = result
            };
        }

        public async Task<IEnumerable<AudioMetaData>> TracksForReleaseAsync(string artist, string Release)
        {
            if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(Release)) return null;
            var result = new List<AudioMetaData>();

            try
            {
                var responseCacheKey = string.Format("uri:lastFm:artistAndRelease:{0}:{1}", artist, Release);
                var releaseInfo = CacheManager.Get<LastAlbum>(responseCacheKey);
                if (releaseInfo == null)
                    try
                    {
                        var auth = new LastAuth(ApiKey.Key, ApiKey.KeySecret);
                        var albumApi = new AlbumApi(auth); // this is an unauthenticated call to the API
                        var response = await albumApi.GetInfoAsync(artist, Release).ConfigureAwait(false);
                        releaseInfo = response.Content;
                        if (releaseInfo != null) CacheManager.Add(responseCacheKey, releaseInfo);
                    }
                    catch
                    {
                        Logger.LogWarning("LastFmAPI: Error Getting Tracks For Artist [{0}], Release [{1}]", artist,
                            Release);
                    }

                if (releaseInfo != null && releaseInfo.Tracks != null && releaseInfo.Tracks.Any())
                {
                    var tracktotal = releaseInfo.Tracks.Where(x => x.Rank.HasValue).Max(x => x.Rank);
                    List<AudioMetaDataImage> images = null;
                    if (releaseInfo.Images != null)
                        images = releaseInfo.Images.Select(x => new AudioMetaDataImage
                        {
                            Url = x.AbsoluteUri
                        }).ToList();
                    foreach (var track in releaseInfo.Tracks)
                        result.Add(new AudioMetaData
                        {
                            Artist = track.ArtistName,
                            Release = track.AlbumName,
                            Title = track.Name,
                            Year = releaseInfo.ReleaseDateUtc != null
                                ? (int?)releaseInfo.ReleaseDateUtc.Value.Year
                                : null,
                            TrackNumber = (short?)track.Rank,
                            TotalTrackNumbers = tracktotal,
                            Time = track.Duration,
                            LastFmId = track.Id,
                            ReleaseLastFmId = releaseInfo.Id,
                            ReleaseMusicBrainzId = releaseInfo.Mbid,
                            MusicBrainzId = track.Mbid,
                            Images = images
                        });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    string.Format("LastFm: Error Getting Tracks For Artist [{0}], Release [{1}]", artist, Release));
            }

            return result;
        }

        protected internal virtual XPathNavigator GetResponseXml(HttpWebRequest request)
        {
            WebResponse response;
            XPathNavigator navigator;
            try
            {
                response = request.GetResponse();
                navigator = GetXpathDocumentFromResponse(response);
                CheckLastFmStatus(navigator);
            }
            catch (WebException exception)
            {
                response = exception.Response;

                XPathNavigator document;
                TryGetXpathDocumentFromResponse(response, out document);

                if (document != null) CheckLastFmStatus(document, exception);
                throw; // throw even if Last.fm status is OK
            }

            return navigator;
        }

        protected internal virtual async Task<XPathNavigator> GetResponseAsXml(HttpResponseMessage request)
        {
            XPathNavigator navigator;
            try
            {
                navigator = await GetXpathDocumentFromResponse(request);
                CheckLastFmStatus(navigator);
            }
            catch (WebException exception)
            {
                var response = exception.Response;

                XPathNavigator document;
                TryGetXpathDocumentFromResponse(response, out document);

                if (document != null) CheckLastFmStatus(document, exception);
                throw; // throw even if Last.fm status is OK
            }

            return navigator;
        }

        protected virtual async Task<XPathNavigator> GetXpathDocumentFromResponse(HttpResponseMessage response)
        {
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                if (stream == null) throw new InvalidOperationException("Response Stream is null");

                try
                {
                    return new XPathDocument(stream).CreateNavigator();
                }
                catch (XmlException exception)
                {
                    throw new XmlException("Could not read HTTP Response as XML", exception);
                }
            }
        }

        protected virtual XPathNavigator GetXpathDocumentFromResponse(WebResponse response)
        {
            using (var stream = response.GetResponseStream())
            {
                if (stream == null) throw new InvalidOperationException("Response Stream is null");

                try
                {
                    return new XPathDocument(stream).CreateNavigator();
                }
                catch (XmlException exception)
                {
                    throw new XmlException("Could not read HTTP Response as XML", exception);
                }
                finally
                {
                    response.Close();
                }
            }
        }

        protected virtual bool TryGetXpathDocumentFromResponse(WebResponse response, out XPathNavigator document)
        {
            bool parsed;

            try
            {
                document = GetXpathDocumentFromResponse(response);
                parsed = true;
            }
            catch (Exception)
            {
                document = null;
                parsed = false;
            }

            return parsed;
        }

        private string BuildUrl(string method, IDictionary<string, string> parameters = null)
        {
            var builder = new StringBuilder($"http://ws.audioscrobbler.com/2.0?method={method}");
            if (parameters != null)
            {
                parameters.Add("api_key", _apiKey.Key);
                foreach (var kv in parameters.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                    builder.Append($"&{kv.Key}={kv.Value}");
                var signature = GenerateMethodSignature(method, parameters);
                builder.Append($"&api_sig={signature} ");
            }

            return builder.ToString();
        }

        private string GenerateMethodSignature(string method, IDictionary<string, string> parameters = null, string sk = null)
        {
            if (parameters == null)
            {
                parameters = new Dictionary<string, string>();
            }
            if (!parameters.ContainsKey("method"))
            {
                parameters.Add("method", method);
            }
            if (!parameters.ContainsKey("api_key"))
            {
                parameters.Add("api_key", _apiKey.Key);
            }
            if (!string.IsNullOrEmpty(sk) && !parameters.ContainsKey("sk"))
            {
                parameters.Add("sk", sk);
            }
            var builder = new StringBuilder();
            foreach (var kv in parameters.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                builder.Append($"{kv.Key}{kv.Value}");
            }
            builder.Append(_apiKey.KeySecret);
            return HashHelper.CreateMD5(builder.ToString());
        }
    }
}
