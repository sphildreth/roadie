using Roadie.Library.Imaging;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;


namespace Roadie.Library.Tests
{
    public class WebHelperTests
    {
        [Fact]
        public void DownloadTestImage()
        {
            var testImageUrl = @"https://i.ytimg.com/vi/OiH5YMXQwYg/maxresdefault.jpg";
            var imageBytes = WebHelper.BytesForImageUrl(testImageUrl);
            Assert.NotNull(imageBytes);
            Assert.NotEmpty(imageBytes);

            var coverFileName = Path.Combine(@"C:\roadie_dev_root", "testImage.jpg");
            File.WriteAllBytes(coverFileName, imageBytes);

        }
    }
}
