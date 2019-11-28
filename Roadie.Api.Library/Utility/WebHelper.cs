using HtmlAgilityPack;
using Roadie.Library.Enums;
using Roadie.Library.Imaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Roadie.Library.Utility
{
    public static class WebHelper
    {
        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.100 Safari/537.36";

        public static byte[] BytesForImageUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Referer = "http://www.roadie.rocks";
                request.UserAgent = UserAgent;

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (BinaryReader reader = new BinaryReader(response.GetResponseStream()))
                    {
                        return reader.ReadBytes(1 * 1024 * 1024 * 10);
                    }
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
                    imageBytes = await webClient.DownloadDataTaskAsync(new Uri(url));
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