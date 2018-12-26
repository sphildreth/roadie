using Roadie.Library.Configuration;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Roadie.Library.Imaging
{
    /// <summary>
    /// Processor that takes images and manipulates
    /// </summary>
    public sealed class ImageProcessor : IDisposable
    {
        private readonly IRoadieSettings _configuration;
        private IntPtr nativeResource = Marshal.AllocHGlobal(100);

        ///// <summary>
        ///// Read from Configuration image encoding; if not set uses default (Jpg Quality of 90)
        ///// </summary>
        //public ImageEncoding ImageEncoding
        //{
        //    get
        //    {
        //        var imageEncoding = ConfigurationManager.AppSettings["ImageProcessor:ImageEncoding"];
        //        if (!string.IsNullOrEmpty(imageEncoding))
        //        {
        //            return (ImageEncoding)Enum.Parse(typeof(ImageEncoding), imageEncoding);
        //        }
        //        return ImageEncoding.Jpg90;
        //    }
        //}

        /// <summary>
        /// Read from Configuration maximum width; if not set uses default (500)
        /// </summary>
        public int MaxWidth
        {
            get
            {
                return this.Configuration.Processing.MaxImageWidth;
            }
        }

        private IRoadieSettings Configuration
        {
            get
            {
                return this._configuration;
            }
        }

        /// <summary>
        /// Processor that takes images and performs any manipulations
        /// </summary>
        public ImageProcessor(IRoadieSettings configuration)
        {
            this._configuration = configuration;
        }

        /// <summary>
        /// Perform any necessary adjustments to file
        /// </summary>
        /// <param name="file">Filename to modify</param>
        /// <returns>Success</returns>
        public bool Process(string file)
        {
            File.WriteAllBytes(file, this.Process(File.ReadAllBytes(file)));
            return true;
        }

        /// <summary>
        /// Perform any necessary adjustments to byte array writing modified file to filename
        /// </summary>
        /// <param name="filename">Filename to Write Modified Byte Array to</param>
        /// <param name="imageBytes">Byte Array of Image To Manipulate</param>
        /// <returns>Success</returns>
        public bool Process(string filename, byte[] imageBytes)
        {
            File.WriteAllBytes(filename, this.Process(imageBytes));
            return true;
        }

        /// <summary>
        /// Perform any necessary adjustments to byte array returning modified array
        /// </summary>
        /// <param name="imageBytes">Byte Array of Image To Manipulate</param>
        /// <returns>Modified Byte Array of Image</returns>
        public byte[] Process(byte[] imageBytes)
        {
            //using (var resizer = new ImageResizer(imageBytes))
            //{
            //    return resizer.Resize(this.MaxWidth, this.ImageEncoding);
            //}

            return ImageHelper.ResizeImage(imageBytes, this.MaxWidth, this.MaxWidth);
        }

        #region IDisposable Implementation

        ~ImageProcessor()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            if (nativeResource != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(nativeResource);
                nativeResource = IntPtr.Zero;
            }
        }

        #endregion IDisposable Implementation

        ///// <summary>
        ///// Fetch Image from Given Url and Return Image
        ///// </summary>
        ///// <param name="url">FQDN of Url to Image</param>
        ///// <returns>Image</returns>
        //public static Image GetImageFromUrl(string url)
        //{
        //    HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);

        //    using (HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse())
        //    {
        //        using (Stream stream = httpWebReponse.GetResponseStream())
        //        {
        //            return Image.FromStream(stream);
        //        }
        //    }
        //}

        ///// <summary>
        ///// Get all Bytes for an Image
        ///// </summary>
        ///// <param name="imageIn">Image to Get Bytes For</param>
        ///// <returns>Byte Array of Image</returns>
        //public static byte[] ImageToByteArray(Image imageIn)
        //{
        //    using (var ms = new MemoryStream())
        //    {
        //        imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
        //        return ms.ToArray();
        //    }
        //}
    }
}