using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Objects;
using Microsoft.Extensions.Logging;
using RestSharp;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using data = Roadie.Library.Data;

namespace Roadie.Library.MetaData.LastFm
{
    public class LastFmHelper : MetaDataProviderBase, IArtistSearchEngine, IReleaseSearchEngine, ILastFmHelper
    {
        private const string LastFmErrorCodeXPath = "/lfm/error/@code";
        private const string LastFmErrorXPath = "/lfm/error";
        private const string LastFmStatusOk = "ok";
        private const string LastFmStatusXPath = "/lfm/@status";

        public override bool IsEnabled =>
            Configuration.Integrations.LastFmProviderEnabled &&
            !string.IsNullOrEmpty(Configuration.Integrations.LastFMApiKey) &&
            !string.IsNullOrEmpty(Configuration.Integrations.LastFmApiSecret);

        private data.IRoadieDbContext DbContext { get; }

        private IHttpEncoder HttpEncoder { get; }

        public LastFmHelper(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger,
                                    data.IRoadieDbContext dbContext, IHttpEncoder httpEncoder)
            : base(configuration, cacheManager, logger)
        {
            _apiKey = configuration.Integrations.ApiKeys.FirstOrDefault(x => x.ApiName == "LastFMApiKey") ??
                      new ApiKey();
            DbContext = dbContext;
            HttpEncoder = httpEncoder;
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

        // http://msdn.microsoft.com/en-us/library/system.security.cryptography.md5.aspx
        // Hash an input string and return the hash as
        // a 32 character hexadecimal string.
        public static string Hash(string input)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.
            using (var md5Hasher = MD5.Create())
            {
                var data = md5Hasher.ComputeHash(System.Text.Encoding.ASCII.GetBytes(input));
                var sb = new StringBuilder();
                foreach (var b in data) sb.Append(b.ToString("X2"));
                return sb.ToString();
            }
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
            var parameters = new Dictionary<string, string>
            {
                {"token", token}
            };
            var request = new RestRequest(Method.GET);
            var client = new RestClient(BuildUrl("auth.getSession", parameters));
            var responseXML = await client.ExecuteTaskAsync<string>(request);
            var doc = new XmlDocument();
            doc.LoadXml(responseXML.Content);
            var sessionKey = doc.GetElementsByTagName("key")[0].InnerText;
            return new OperationResult<string>
            {
                Data = sessionKey,
                IsSuccess = true
            };
        }

        public async Task<OperationResult<bool>> NowPlaying(User roadieUser, ScrobbleInfo scrobble)
        {
            var result = false;
            try
            {
                if (!IsEnabled) return new OperationResult<bool>("LastFM Integation Disabled");

                var user = DbContext.Users.FirstOrDefault(x => x.RoadieId == roadieUser.UserId);

                if (user == null || string.IsNullOrEmpty(user.LastFMSessionKey))
                    return new OperationResult<bool>("User does not have LastFM Integration setup");
                var method = "track.updateNowPlaying";
                var parameters = new RequestParameters
                {
                    {"artist", scrobble.ArtistName},
                    {"track", scrobble.TrackTitle},
                    {"album", scrobble.ReleaseTitle},
                    {"duration", ((int) scrobble.TrackDuration.TotalSeconds).ToString()}
                };
                var url = "http://ws.audioscrobbler.com/2.0/";
                var signature = GenerateMethodSignature(method, parameters, user.LastFMSessionKey);
                parameters.Add("api_sig", signature);

                ServicePointManager.Expect100Continue = false;
                var request = WebRequest.Create(url);
                request.Method = "POST";
                var postData = parameters.ToString();
                var byteArray = System.Text.Encoding.UTF8.GetBytes(postData);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                using (var dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                }

                var xp = GetResponseAsXml(request);
                Logger.LogInformation(
                    $"LastFmHelper: RoadieUser `{roadieUser}` NowPlaying `{scrobble}` LastFmResult [{xp.InnerXml}]");
                result = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    $"Error in LastFmHelper NowPlaying: RoadieUser `{roadieUser}` Scrobble `{scrobble}`");
            }

            return new OperationResult<bool>
            {
                IsSuccess = result,
                Data = result
            };
        }

        public async Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearch(string query,
                                                            int resultsCount)
        {
            try
            {
                Logger.LogTrace("LastFmHelper:PerformArtistSearch:{0}", query);
                var auth = new LastAuth(ApiKey.Key, ApiKey.KeySecret);
                var albumApi = new ArtistApi(auth);
                var response = await albumApi.GetInfoAsync(query);
                if (!response.Success) return new OperationResult<IEnumerable<ArtistSearchResult>>();
                var lastFmArtist = response.Content;
                var result = new ArtistSearchResult
                {
                    ArtistName = lastFmArtist.Name,
                    LastFMId = lastFmArtist.Id,
                    MusicBrainzId = lastFmArtist.Mbid,
                    Bio = lastFmArtist.Bio != null ? lastFmArtist.Bio.Content : null
                };
                if (lastFmArtist.Tags != null) result.Tags = lastFmArtist.Tags.Select(x => x.Name).ToList();
                // No longer fetching/consuming images LastFm says is violation of ToS ; https://getsatisfaction.com/lastfm/topics/api-announcement-dac8oefw5vrxq
                if (lastFmArtist.Url != null) result.Urls = new[] { lastFmArtist.Url.ToString() };
                return new OperationResult<IEnumerable<ArtistSearchResult>>
                {
                    IsSuccess = response.Success,
                    Data = new List<ArtistSearchResult> { result }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
            }

            return new OperationResult<IEnumerable<ArtistSearchResult>>();
        }

        public async Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName,
            string query, int resultsCount)
        {
            var request = new RestRequest(Method.GET);
            var client = new RestClient(string.Format(
                "http://ws.audioscrobbler.com/2.0/?method=album.getinfo&api_key={0}&artist={1}&album={2}&format=xml",
                ApiKey.Key, artistName, query));
            var responseData = await client.ExecuteTaskAsync<lfm>(request);

            ReleaseSearchResult result = null;

            var response = responseData != null && responseData.Data != null ? responseData.Data : null;
            if (response != null && response.album != null)
            {
                var lastFmAlbum = response.album;
                result = new ReleaseSearchResult
                {
                    ReleaseTitle = lastFmAlbum.name,
                    MusicBrainzId = lastFmAlbum.mbid
                };

                // No longer fetching/consuming images LastFm says is violation of ToS ; https://getsatisfaction.com/lastfm/topics/api-announcement-dac8oefw5vrxq

                if (lastFmAlbum.tags != null) result.Tags = lastFmAlbum.tags.Select(x => x.name).ToList();
                if (lastFmAlbum.tracks != null)
                {
                    var tracks = new List<TrackSearchResult>();
                    foreach (var lastFmTrack in lastFmAlbum.tracks)
                        tracks.Add(new TrackSearchResult
                        {
                            TrackNumber = SafeParser.ToNumber<short?>(lastFmTrack.rank),
                            Title = lastFmTrack.name,
                            Duration = SafeParser.ToNumber<int?>(lastFmTrack.duration),
                            Urls = string.IsNullOrEmpty(lastFmTrack.url) ? new[] { lastFmTrack.url } : null
                        });
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

            return new OperationResult<IEnumerable<ReleaseSearchResult>>
            {
                IsSuccess = result != null,
                Data = new List<ReleaseSearchResult> { result }
            };
        }

        public async Task<OperationResult<bool>> Scrobble(User roadieUser, ScrobbleInfo scrobble)
        {
            var result = false;
            try
            {
                if (!IsEnabled) return new OperationResult<bool>("LastFM Integation Disabled");
                // LastFM Rules on scrobbling:
                // * The track must be longer than 30 seconds.
                // * And the track has been played for at least half its duration, or for 4 minutes(whichever occurs earlier.)
                if (scrobble.TrackDuration.TotalSeconds < 30)
                    return new OperationResult<bool>(
                        "Track duration or elapsed time does not qualify for LastFM Scrobbling");
                // If less than half of duration then create a NowPlaying
                if (scrobble.ElapsedTimeOfTrackPlayed.TotalMinutes < 4 ||
                    scrobble.ElapsedTimeOfTrackPlayed.TotalSeconds < scrobble.TrackDuration.TotalSeconds / 2)
                    return await NowPlaying(roadieUser, scrobble);

                var user = DbContext.Users.FirstOrDefault(x => x.RoadieId == roadieUser.UserId);

                if (user == null || string.IsNullOrEmpty(user.LastFMSessionKey))
                    return new OperationResult<bool>("User does not have LastFM Integration setup");
                var parameters = new RequestParameters
                {
                    {"artist", scrobble.ArtistName},
                    {"track", scrobble.TrackTitle},
                    {"timestamp", scrobble.TimePlayed.ToUnixTimeSinceEpoch().ToString()},
                    {"album", scrobble.ReleaseTitle},
                    {"chosenByUser", scrobble.IsRandomizedScrobble ? "1" : "0"},
                    {"duration", ((int) scrobble.TrackDuration.TotalSeconds).ToString()}
                };

                var method = "track.scrobble";
                var url = "http://ws.audioscrobbler.com/2.0/";
                var signature = GenerateMethodSignature(method, parameters, user.LastFMSessionKey);
                parameters.Add("api_sig", signature);

                ServicePointManager.Expect100Continue = false;
                var request = WebRequest.Create(url);
                request.Method = "POST";
                var postData = parameters.ToString();
                var byteArray = System.Text.Encoding.UTF8.GetBytes(postData);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                using (var dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                }

                var xp = GetResponseAsXml(request);
                Logger.LogInformation(
                    $"LastFmHelper: RoadieUser `{roadieUser}` Scrobble `{scrobble}` LastFmResult [{xp.InnerXml}]");
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

        public async Task<IEnumerable<AudioMetaData>> TracksForRelease(string artist, string Release)
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
                        var response = await albumApi.GetInfoAsync(artist, Release);
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

        protected internal virtual XPathNavigator GetResponseAsXml(WebRequest request)
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

        private string GenerateMethodSignature(string method, IDictionary<string, string> parameters = null,
            string sk = null)
        {
            if (parameters == null) parameters = new Dictionary<string, string>();
            if (!parameters.ContainsKey("method")) parameters.Add("method", method);
            if (!parameters.ContainsKey("api_key")) parameters.Add("api_key", _apiKey.Key);
            if (!string.IsNullOrEmpty(sk) && !parameters.ContainsKey("sk")) parameters.Add("sk", sk);
            var builder = new StringBuilder();
            foreach (var kv in parameters.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                builder.Append($"{kv.Key}{kv.Value}");
            builder.Append(_apiKey.KeySecret);
            return Hash(builder.ToString());
        }
    }
}