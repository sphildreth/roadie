using Roadie.Library.Utility;
using System;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Roadie.Library.MetaData.MusicBrainz
{
    public static class MusicBrainzRequestHelper
    {
        private const string LookupTemplate = "{0}/{1}/?inc={2}&fmt=json&limit=100";
        private const int MaxRetries = 6;
        private const string ReleaseBrowseTemplate = "release?artist={0}&limit={1}&offset={2}&fmt=json&inc={3}";
        private const string SearchTemplate = "{0}?query={1}&limit={2}&offset={3}&fmt=json";
        private const string WebServiceUrl = "http://musicbrainz.org/ws/2/";

        internal static string CreateArtistBrowseTemplate(string id, int limit, int offset)
        {
            return string.Format("{0}{1}", WebServiceUrl, string.Format(ReleaseBrowseTemplate, id, limit, offset, "labels+aliases+recordings+release-groups+media+url-rels+tags+genres"));
        }

        internal static string CreateCoverArtReleaseUrl(string musicBrainzId)
        {
            return string.Format("http://coverartarchive.org/release/{0}", musicBrainzId);
        }

        /// <summary>
        ///     Creates a webservice lookup template.
        /// </summary>
        internal static string CreateLookupUrl(string entity, string mbid, string inc)
        {
            return string.Format("{0}{1}", WebServiceUrl, string.Format(LookupTemplate, entity, mbid, inc));
        }

        /// <summary>
        ///     Creates a webservice search template.
        /// </summary>
        internal static string CreateSearchTemplate(string entity, string query, int limit, int offset)
        {
            query = Uri.EscapeDataString(query);

            return string.Format("{0}{1}", WebServiceUrl, string.Format(SearchTemplate, entity, query, limit, offset));
        }

        internal static async Task<T> GetAsync<T>(string url, bool withoutMetadata = true)
        {
            var tryCount = 0;
            var result = default(T);
            string downloadedString = null;
            while (result == null && tryCount < MaxRetries)
            {
                try
                {
                    using (var webClient = new WebClient())
                    {
                        webClient.Headers.Add("user-agent", WebHelper.UserAgent);
                        downloadedString = await webClient.DownloadStringTaskAsync(new Uri(url)).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(downloadedString))
                        {
                            result = JsonSerializer.Deserialize<T>(downloadedString);
                        }
                    }
                }
                catch (WebException ex)
                {
                    var response = ex.Response as HttpWebResponse;
                    if(response?.StatusCode == HttpStatusCode.NotFound)
                    {
                        Trace.WriteLine($"GetAsync: 404 Response For url [{ url }]", "Warning");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"GetAsync: DownloadedString [{ downloadedString }], Exception: [{ ex }]", "Warning");
                    Thread.Sleep(100);
                }
                finally
                {
                    tryCount++;
                }
            }

            return result;
        }
    }
}