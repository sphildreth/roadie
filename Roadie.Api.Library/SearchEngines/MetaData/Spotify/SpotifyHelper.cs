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

namespace Roadie.Library.SearchEngines.MetaData.Spotify
{
    public class SpotifyHelper : MetaDataProviderBase, ISpotifyHelper
    {
        public override bool IsEnabled => Configuration.Integrations.SpotifyProviderEnabled;

        public SpotifyHelper(IRoadieSettings configuration, ICacheManager cacheManager, ILogger<SpotifyHelper> logger)
                    : base(configuration, cacheManager, logger)
        {
        }

        public async Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearch(string query,
            int resultsCount)
        {
            ArtistSearchResult data = null;
            try
            {
                // TODO update this to use https://github.com/JohnnyCrazy/SpotifyAPI-NET

                Logger.LogTrace("SpotifyHelper:PerformArtistSearch:{0}", query);
                var request = BuildSearchRequest(query, 1, "artist");

                var client = new RestClient("http://api.spotify.com/v1");
                client.UserAgent = WebHelper.UserAgent;

                var response = await client.ExecuteTaskAsync<SpotifyResult>(request);

                if (response.ResponseStatus == ResponseStatus.Error)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                        throw new AuthenticationException("Unauthorized");
                    throw new Exception(string.Format("Request Error Message: {0}. Content: {1}.",
                        response.ErrorMessage, response.Content));
                }

                Item spotifyArtist = null;
                if (response.Data != null && response.Data.artists != null && response.Data.artists.items != null)
                    spotifyArtist = response.Data.artists.items.FirstOrDefault(x =>
                        x.name.Equals(query, StringComparison.OrdinalIgnoreCase));
                if (spotifyArtist == null) return new OperationResult<IEnumerable<ArtistSearchResult>>();
                data = new ArtistSearchResult
                {
                    ArtistName = spotifyArtist.name,
                    ArtistType = spotifyArtist.type,
                    SpotifyId = spotifyArtist.id
                };
                if (spotifyArtist.images != null)
                    data.ImageUrls = spotifyArtist.images.OrderByDescending(x => x.height).Take(1).Select(x => x.url)
                        .ToList();
                if (spotifyArtist.genres != null) data.ArtistGenres = spotifyArtist.genres.Select(x => x).ToList();
                if (spotifyArtist.external_urls != null) data.Urls = new[] { spotifyArtist.external_urls.spotify };
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

        public async Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName,
            string query, int resultsCount)
        {
            var artistResult = await PerformArtistSearch(artistName, resultsCount);
            if (!artistResult.IsSuccess) return new OperationResult<IEnumerable<ReleaseSearchResult>>();
            try
            {
                var tcs = new TaskCompletionSource<OperationResult<IEnumerable<ReleaseSearchResult>>>();
                var request = new RestRequest(Method.GET);

                ReleaseSearchResult result = null;

                var response = await AlbumsForArtist(artistResult.Data.First().SpotifyId);
                if (response != null && response.items != null)
                {
                    string foundByAlternateName = null;
                    var spotifyAlbum = response.items.FirstOrDefault(x =>
                        x.name.Trim().Equals(query.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (spotifyAlbum == null)
                    {
                        // No Exact match see if one starts with and use the first one
                        spotifyAlbum = response.items.FirstOrDefault(x =>
                            x.name.Trim().StartsWith(query.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (spotifyAlbum == null) return new OperationResult<IEnumerable<ReleaseSearchResult>>();
                        foundByAlternateName = spotifyAlbum.name;
                    }

                    var client = new RestClient(string.Format("http://api.spotify.com/v1/albums/{0}", spotifyAlbum.id));
                    var albumResult = await client.ExecuteTaskAsync<AlbumResult>(request);
                    if (albumResult != null && albumResult.Data != null)
                    {
                        var sa = albumResult.Data;
                        var resultTags = new List<string>();
                        var resultUrls = new List<string>();
                        if (sa.external_ids != null) resultTags.Add("upc:" + sa.external_ids.upc);
                        result = new ReleaseSearchResult
                        {
                            ReleaseTitle = !string.IsNullOrEmpty(foundByAlternateName)
                                ? query.ToTitleCase(false)
                                : sa.name,
                            ReleaseDate = SafeParser.ToDateTime(sa.release_date),
                            ReleaseType = sa.album_type,
                            SpotifyId = sa.id,
                            Tags = resultTags,
                            AlternateNames = !string.IsNullOrEmpty(foundByAlternateName)
                                ? new[] { foundByAlternateName }
                                : null
                        };
                        if (sa.artists != null && sa.artists.FirstOrDefault() != null)
                        {
                            var saArtist = sa.artists.First();
                            result.Artist = new ArtistSearchResult
                            {
                                ArtistName = saArtist.name,
                                ArtistType = saArtist.type,
                                SpotifyId = saArtist.id
                            };
                        }

                        if (sa.genres != null) result.ReleaseGenres = sa.genres.ToList();
                        if (sa.external_urls != null) resultUrls.Add(sa.external_urls.spotify);
                        if (sa.images != null)
                            result.ImageUrls = sa.images.OrderBy(x => x.width).Take(1).Select(x => x.url).ToList();
                        if (sa.tracks != null)
                        {
                            var releaseMediaCount = sa.tracks.items.Max(x => x.disc_number ?? 0);
                            var releaseMedias = new List<ReleaseMediaSearchResult>();
                            for (short? i = 1; i <= releaseMediaCount; i++)
                            {
                                var releaseTracks = new List<TrackSearchResult>();
                                foreach (var saTrack in sa.tracks.items)
                                {
                                    ArtistSearchResult trackArtist = null;
                                    if (saTrack.artists != null && saTrack.artists.FirstOrDefault() != null)
                                    {
                                        var saTrackArtist = saTrack.artists.First();
                                        trackArtist = new ArtistSearchResult
                                        {
                                            ArtistName = saTrackArtist.name,
                                            SpotifyId = saTrackArtist.id,
                                            ArtistType = saTrackArtist.type
                                        };
                                    }

                                    releaseTracks.Add(new TrackSearchResult
                                    {
                                        Artist = !trackArtist.SpotifyId.Equals(trackArtist.SpotifyId)
                                            ? trackArtist
                                            : null,
                                        TrackNumber = saTrack.track_number,
                                        Title = saTrack.name,
                                        SpotifyId = saTrack.id,
                                        Urls = new List<string> { saTrack.external_urls.spotify, saTrack.preview_url },
                                        Duration = saTrack.duration_ms,
                                        TrackType = saTrack.type
                                    });
                                }

                                releaseMedias.Add(new ReleaseMediaSearchResult
                                {
                                    ReleaseMediaNumber = i,
                                    TrackCount = (short)sa.tracks.items.Count(x => x.disc_number == i),
                                    Tracks = releaseTracks
                                });
                            }

                            result.ReleaseMedia = releaseMedias;
                        }

                        result.Urls = resultUrls;
                    }
                }

                return new OperationResult<IEnumerable<ReleaseSearchResult>>
                {
                    IsSuccess = result != null,
                    Data = new List<ReleaseSearchResult> { result }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
            }

            return new OperationResult<IEnumerable<ReleaseSearchResult>>();
        }

        private async Task<Albums> AlbumsForArtist(string spotifyId)
        {
            var cacheKey = string.Format("uri:spotify:AlbumsForArtist:{0}", spotifyId);
            var result = CacheManager.Get<Albums>(cacheKey);
            if (result == null)
            {
                var request = new RestRequest(Method.GET);
                var client = new RestClient(string.Format(
                    "http://api.spotify.com/v1/artists/{0}/albums?offset=0&limit=25&album_type=album&market=US",
                    spotifyId));
                var artistAlbumsResponse = await client.ExecuteTaskAsync<Albums>(request);
                result = artistAlbumsResponse != null && artistAlbumsResponse.Data != null
                    ? artistAlbumsResponse.Data
                    : null;
                if (result != null) CacheManager.Add(cacheKey, result);
            }

            return result;
        }

        private RestRequest BuildSearchRequest(string query, int resultsCount, string entityType)
        {
            var request = new RestRequest
            {
                Resource = "search",
                Method = Method.GET,
                RequestFormat = DataFormat.Json
            };
            request.AddParameter(new Parameter
            {
                Name = "type",
                Value = entityType,
                Type = ParameterType.GetOrPost
            });
            request.AddParameter(new Parameter
            {
                Name = "q",
                Value = string.Format("{0}", query.Trim()),
                Type = ParameterType.GetOrPost
            });
            request.AddParameter(new Parameter
            {
                Name = "market",
                Value = "US",
                Type = ParameterType.GetOrPost
            });
            return request;
        }
    }
}