using Roadie.Library.Data;
using Roadie.Library.Enums;
using Roadie.Library.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Roadie.Library.Utility
{
    public static class WebHelper
    {
        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36";

        public static async Task<Image> GetImageFromUrlAsync(string url)
        {
            byte[] imageBytes = null;
            try
            {
                using (var webClient = new WebClient())
                {
                    webClient.Headers.Add("user-agent", WebHelper.UserAgent);
                    imageBytes = await webClient.DownloadDataTaskAsync(new Uri(url));
                }
            }
            catch
            {
            }
            if (imageBytes != null)
            {
                var signature = ImageHasher.AverageHash(imageBytes).ToString();
                var ib = ImageHelper.ConvertToJpegFormat(imageBytes);
                return new Image
                {
                    Url = url,
                    Status = Statuses.New,
                    RoadieId = Guid.NewGuid(),
                    Signature = signature,
                    Bytes = ib
                };
            }
            return null;
        }

        public static byte[] BytesForImageUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }
            try
            {
                using (var webClient = new WebClient())
                {
                    return webClient.DownloadData(url);
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
                    var htmlDoc = new HtmlAgilityPack.HtmlDocument();
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
                Trace.WriteLine(string.Format("Error with url [{0}] Exception [{1}]", url, ex.ToString()));
            }
            return null;
        }

        public static bool IsStringUrl(string uriName)
        {
            try
            {
                if (string.IsNullOrEmpty(uriName))
                {
                    return false;
                }
                return uriName.ToLower().StartsWith("http://") || uriName.ToLower().StartsWith("https://");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            return false;
        }
    }
}
