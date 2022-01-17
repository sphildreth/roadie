using HtmlAgilityPack;
using Roadie.Library.Enums;
using Roadie.Library.Imaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Roadie.Library.Utility
{
    public static class WebHelper
    {
        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:96.0) Gecko/20100101 Firefox/96.0";

        public static async Task<byte[]> BytesForImageUrl(IHttpClientFactory httpclientFactory, string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }
            try
            {
                var client = httpclientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await client.SendAsync(request).ConfigureAwait(false);
                if(response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                }
            }
            catch (WebException wex)
            {
                var err = "";
                try
                {
                    using (var sr = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        err = sr.ReadToEnd();
                    }

                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(err);
                    err = (htmlDoc.DocumentNode.InnerText ?? string.Empty).Trim();
                }
                catch (Exception)
                {
                }

                throw new Exception(err);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("Error with url [{0}] Exception [{1}]", url, ex), "Warning");
            }

            return null;
        }

        public static async Task<IImage> GetImageFromUrlAsync(string url)
        {
            byte[] imageBytes = null;
            try
            {
                using (var webClient = new WebClient())
                {
                    webClient.Headers.Add("user-agent", UserAgent);
                    imageBytes = await webClient.DownloadDataTaskAsync(new Uri(url)).ConfigureAwait(false);
                }
            }
            catch
            {
            }

            try
            {
                if (imageBytes != null)
                {
                    var signature = ImageHasher.AverageHash(imageBytes).ToString();
                    var ib = ImageHelper.ConvertToJpegFormat(imageBytes);
                    return new Image(Guid.NewGuid())
                    {
                        Url = url,
                        Status = Statuses.New,
                        Signature = signature,
                        Bytes = ib
                    };
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"GetImageFromUrlAsync Url [{ url }], Exception [{ ex.ToString() }", "Warning");
            }
            return null;
        }

        public static bool IsStringUrl(string uriName)
        {
            try
            {
                if (string.IsNullOrEmpty(uriName)) return false;
                return uriName.ToLower().StartsWith("http://") || uriName.ToLower().StartsWith("https://");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error");
            }

            return false;
        }
    }
}