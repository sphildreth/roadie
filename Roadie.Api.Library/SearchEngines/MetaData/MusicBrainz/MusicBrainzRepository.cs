using LiteDB;
using Microsoft.Extensions.Logging;
using Roadie.Library.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Roadie.Library.MetaData.MusicBrainz
{
    public class MusicBrainzRepository
    {
        private string FileName { get; }

        private ILogger<MusicBrainzProvider> Logger { get; }

        private Lazy<HttpClient> _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;

        private HttpClient HttpClient => _httpClient.Value;

        public MusicBrainzRepository(
            IRoadieSettings configuration,
            ILogger<MusicBrainzProvider> logger,
            IHttpClientFactory httpClientFactory)
        {
            Logger = logger;
            var location = System.Reflection.Assembly.GetEntryAssembly().Location;
            var directory = configuration.SearchEngineReposFolder ?? Path.Combine(System.IO.Path.GetDirectoryName(location), "SearchEngineRepos");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            FileName = Path.Combine(directory, "MusicBrainzRespository.db");
            _httpClientFactory = httpClientFactory;
            _httpClient = new Lazy<HttpClient>(() => _httpClientFactory.CreateClient());
        }

        /// <summary>
        /// Return if artist exists in repository, if not fetch from MusicBrainz and populate then return
        /// </summary>
        /// <param name="name">Query name of Artist</param>
        /// <param name="resultsCount">Maximum Number of Results</param>
        public async Task<Artist> ArtistByNameAsync(string name, int? resultsCount = null)
        {
            Artist result = null;

            try
            {
                using (var db = new LiteDatabase(FileName))
                {
                    var col = db.GetCollection<RepositoryArtist>("artists");
                    col.EnsureIndex(x => x.ArtistName);
                    col.EnsureIndex(x => x.ArtistMbId);
                    var artist = col.Find(x => x.ArtistName.Equals(name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (artist == null)
                    {
                        // Perform a query to get the MbId for the Name
                        var artistResult = await MusicBrainzRequestHelper.GetAsync<ArtistResult>(HttpClient, MusicBrainzRequestHelper.CreateSearchTemplate("artist", name, resultsCount ?? 1, 0)).ConfigureAwait(false);
                        if (artistResult == null || artistResult.artists == null || !artistResult.artists.Any() || artistResult.count < 1)
                        {
                            return null;
                        }
                        var mbId = artistResult.artists.First().id;
                        // Now perform a detail request to get the details by the MbId
                        result = await MusicBrainzRequestHelper.GetAsync<Artist>(HttpClient, MusicBrainzRequestHelper.CreateLookupUrl("artist", mbId, "aliases+tags+genres+url-rels")).ConfigureAwait(false);
                        if (result != null)
                        {
                            col.Insert(new RepositoryArtist
                            {
                                ArtistName = name,
                                ArtistMbId = result.id,
                                Artist = result
                            });
                        }
                    }
                    else
                    {
                        result = artist.Artist;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.LogTrace($"MusicBrainzArtist: ArtistName [{ name }], HttpRequestException: [{ ex.ToString() }] ");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                try
                {
                    File.Delete(FileName);
                    Logger.LogWarning($"Deleted corrupt MusicBrainzRepository [{ FileName }].");
                }
                catch
                {
                }
            }
            return result;
        }

        /// <summary>
        /// Return if releases exist in repository for artist, if not fetch from MusicBrainz and populate then return
        /// </summary>
        /// <param name="artistMbId">Artist Music Brainz Id</param>
        /// <returns>Collection of Music Brainz Releases</returns>
        public async Task<IEnumerable<Release>> ReleasesForArtist(string artistMbId)
        {
            IEnumerable<Release> results = null;
            try
            {
                using (var db = new LiteDatabase(FileName))
                {
                    var col = db.GetCollection<RepositoryRelease>("releases");
                    col.EnsureIndex(x => x.ArtistMbId);
                    col.EnsureIndex(x => x.Release.id);
                    var releases = col.Find(x => x.ArtistMbId == artistMbId);
                    if (releases == null || !releases.Any())
                    {
                        // Query to get collection of Releases for Artist
                        var pageSize = 50;
                        var page = 0;
                        var url = MusicBrainzRequestHelper.CreateArtistBrowseTemplate(artistMbId, pageSize, 0);
                        var mbReleaseBrowseResult = await MusicBrainzRequestHelper.GetAsync<ReleaseBrowseResult>(HttpClient, url).ConfigureAwait(false);
                        var totalReleases = mbReleaseBrowseResult != null ? mbReleaseBrowseResult.releasecount : 0;
                        var totalPages = Math.Ceiling((decimal)totalReleases / pageSize);
                        var fetchResult = new List<Release>();
                        do
                        {
                            if (mbReleaseBrowseResult != null)
                            {
                                fetchResult.AddRange(mbReleaseBrowseResult.releases.Where(x => !string.IsNullOrEmpty(x.date)));
                            }
                            page++;
                            mbReleaseBrowseResult = await MusicBrainzRequestHelper.GetAsync<ReleaseBrowseResult>(HttpClient, MusicBrainzRequestHelper.CreateArtistBrowseTemplate(artistMbId, pageSize, pageSize * page)).ConfigureAwait(false);
                        } while (page < totalPages);
                        var releasesToInsert = fetchResult.GroupBy(x => x.title).Select(x => x.OrderBy(x => x.date).First()).OrderBy(x => x.date).ThenBy(x => x.title);
                        col.InsertBulk(releasesToInsert.Where(x => x != null).Select(x => new RepositoryRelease
                        {
                            ArtistMbId = artistMbId,
                            Release = x
                        }));
                        results = releasesToInsert;
                    }
                    else
                    {
                        results = releases.Select(x => x.Release).ToArray();
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.LogTrace($"MusicBrainz:ReleasesForArtist, Artist [{ artistMbId }], Ex [{ ex.ToString() }]");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            return results.OrderBy(x => x.date).ThenBy(x => x.title).ToArray();
        }
    }
}
