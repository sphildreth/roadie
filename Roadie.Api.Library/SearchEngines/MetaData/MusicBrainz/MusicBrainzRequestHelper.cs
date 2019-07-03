using Newtonsoft.Json;
using Roadie.Library.Utility;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Roadie.Library.MetaData.MusicBrainz
{
    public static class MusicBrainzRequestHelper
    {
        private const string LookupTemplate = "{0}/{1}/?inc={2}&fmt=json&limit=100";
        private const int MaxRetries = 6;
        private const string ReleaseBrowseTemplate = "release?artist={0}&limit={1}&offset={2}&fmt=json";
        private const string SearchTemplate = "{0}?query={1}&limit={2}&offset={3}&fmt=json";
        private const string WebServiceUrl = "http://musicbrainz.org/ws/2/";

        internal static string CreateArtistBrowseTemplate(string id, int limit, int offset)
        {
            return string.Format("{0}{1}", WebServiceUrl, string.Format(ReleaseBrowseTemplate, id, limit, offset));
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
            query = Uri.EscapeUriString(query);

            return string.Format("{0}{1}", WebServiceUrl, string.Format(SearchTemplate, entity, query, limit, offset));
        }

        internal static async Task<T> GetAsync<T>(string url, bool withoutMetadata = true)
        {
            var tryCount = 0;
            var result = default(T);
            while (tryCount < MaxRetries && result == null)
                try
                {
                    using (var webClient = new WebClient())
                    {
                        webClient.Headers.Add("user-agent", WebHelper.UserAgent);
                        result = JsonConvert.DeserializeObject<T>(
                            await webClient.DownloadStringTaskAsync(new Uri(url)));
                    }
                }
                catch
                {
                    Thread.Sleep(100);
                }
                finally
                {
                    tryCount++;
                }

            return result;
        }
    }
}