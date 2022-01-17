using Roadie.Library.Utility;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Roadie.Library.Tests
{
    public class WebHelperTests : HttpClientFactoryBaseTests
    {
        [Fact]
        public async Task DownloadTestImage()
        {
            var testImageUrl = @"https://i.ytimg.com/vi/OiH5YMXQwYg/maxresdefault.jpg";
            var imageBytes = await WebHelper.BytesForImageUrl(_httpClientFactory, testImageUrl).ConfigureAwait(false); 
            Assert.NotNull(imageBytes);
            Assert.NotEmpty(imageBytes);

            var coverFileName = Path.Combine(@"C:\roadie_dev_root", "testImage.jpg");
            File.WriteAllBytes(coverFileName, imageBytes);
        }
    }
}