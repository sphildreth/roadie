using Roadie.Library.Caching;
using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Objects;
using RestSharp;
using Roadie.Library.Extensions;
using Roadie.Library.SearchEngines.MetaData;
using Roadie.Library.SearchEngines.MetaData.LastFm;
using Roadie.Library.Utility;
using Roadie.Library.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.Setttings;

namespace Roadie.Library.MetaData.LastFm
{
    public class LastFmHelper : MetaDataProviderBase, IArtistSearchEngine, IReleaseSearchEngine
    {
        public override bool IsEnabled
        {
            get
            {
                return this.Configuration.GetValue<bool>("Integrations:LastFmProviderEnabled", true) &&
                       !string.IsNullOrEmpty(this.ApiKey.Key);
            }
        }

        public LastFmHelper(IConfiguration configuration, ICacheManager cacheManager, ILogger loggingService) : base(configuration, cacheManager, loggingService)
        {
            this._apiKey = configuration.GetValue<List<ApiKey>>("ApiKeys", new List<ApiKey>()).FirstOrDefault(x => x.ApiName == "LastFMApiKey") ?? new ApiKey();
        }

        public async Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearch(string query, int resultsCount)
        {
            try
            {
                this.Logger.Trace("LastFmHelper:PerformArtistSearch:{0}", query);
                var auth = new LastAuth(this.ApiKey.Key, this.ApiKey.Secret);
                var albumApi = new ArtistApi(auth);
                var response = await albumApi.GetInfoAsync(query);
                if (!response.Success)
                {
                    return new OperationResult<IEnumerable<ArtistSearchResult>>();
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
                if (lastFmArtist.MainImage != null && (lastFmArtist.MainImage.ExtraLarge != null || lastFmArtist.MainImage.Large != null ))
                {
                    result.ArtistThumbnailUrl = (lastFmArtist.MainImage.ExtraLarge ?? lastFmArtist.MainImage.Large).ToString();
                }
                if (lastFmArtist.Url != null)
                {
                    result.Urls = new string[] { lastFmArtist.Url.ToString() };
                }
                return new OperationResult<IEnumerable<ArtistSearchResult>>
                {
                    IsSuccess = response.Success,
                    Data = new List<ArtistSearchResult> { result }
                };
        }
            catch (Exception ex)
            {
                this.Logger.Error(ex, ex.Serialize());
            }
            return new OperationResult<IEnumerable<ArtistSearchResult>>();                       
        }

        public async Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName, string query, int resultsCount)
        {
            var request = new RestRequest(Method.GET);
            var client = new RestClient(string.Format("http://ws.audioscrobbler.com/2.0/?method=album.getinfo&api_key={0}&artist={1}&album={2}&format=xml", this.ApiKey.Key, artistName, query));
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

                if (lastFmAlbum.image != null)
                {
                    result.ImageUrls = lastFmAlbum.image.Where(x => x.size == "extralarge").Select(x => x.Value).ToList();
                }
                if (lastFmAlbum.tags != null)
                {
                    result.Tags = lastFmAlbum.tags.Select(x => x.name).ToList();
                }
                if (lastFmAlbum.tracks != null)
                {
                    var tracks = new List<TrackSearchResult>();
                    foreach (var lastFmTrack in lastFmAlbum.tracks)
                    {
                        tracks.Add(new TrackSearchResult
                        {
                            TrackNumber = SafeParser.ToNumber<short?>(lastFmTrack.rank),
                            Title = lastFmTrack.name,
                            Duration = SafeParser.ToNumber<int?>(lastFmTrack.duration),
                            Urls = string.IsNullOrEmpty(lastFmTrack.url) ? new string[] { lastFmTrack.url } : null,
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
            return new OperationResult<IEnumerable<ReleaseSearchResult>>
            {
                IsSuccess = result != null,
                Data = new List<ReleaseSearchResult> { result }
            };
        }

        public async Task<IEnumerable<AudioMetaData>> TracksForRelease(string artist, string Release)
        {
            if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(Release))
            {
                return null;
            }
            var result = new List<AudioMetaData>();

            try
            {
                var responseCacheKey = string.Format("uri:lastFm:artistAndRelease:{0}:{1}", artist, Release);
                LastAlbum releaseInfo = this.CacheManager.Get<LastAlbum>(responseCacheKey);
                if (releaseInfo == null)
                {
                    try
                    {
                        var auth = new LastAuth(this.ApiKey.Key, this.ApiKey.Secret);
                        var albumApi = new AlbumApi(auth); // this is an unauthenticated call to the API
                        var response = await albumApi.GetInfoAsync(artist, Release);
                        releaseInfo = response.Content;
                        if (releaseInfo != null)
                        {
                            this.CacheManager.Add(responseCacheKey, releaseInfo);
                        }
                    }
                    catch
                    {
                        this.Logger.Warning("LastFmAPI: Error Getting Tracks For Artist [{0}], Release [{1}]", artist, Release);
                    }
                }

                if (releaseInfo != null && releaseInfo.Tracks != null && releaseInfo.Tracks.Any())
                {
                    var tracktotal = releaseInfo.Tracks.Where(x => x.Rank.HasValue).Max(x => x.Rank);
                    List<AudioMetaDataImage> images = null;
                    if (releaseInfo.Images != null)
                    {
                        images = releaseInfo.Images.Select(x => new AudioMetaDataImage
                        {
                            Url = x.AbsoluteUri
                        }).ToList();
                    }
                    foreach (var track in releaseInfo.Tracks)
                    {
                        result.Add(new AudioMetaData
                        {
                            Artist = track.ArtistName,
                            Release = track.AlbumName,
                            Title = track.Name,
                            Year = releaseInfo.ReleaseDateUtc != null ? (int?)releaseInfo.ReleaseDateUtc.Value.Year : null,
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
            }
            catch (System.Exception ex)
            {
                this.Logger.Error(ex, string.Format("LastFm: Error Getting Tracks For Artist [{0}], Release [{1}]", artist, Release));
            }
            return result;
        }
    }
}