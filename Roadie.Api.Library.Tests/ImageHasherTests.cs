using Roadie.Library.Imaging;
using System;
using System.IO;
using Xunit;

namespace Roadie.Library.Tests
{
    public class ImageHasherTests
    {
        [Fact]
        public void GenerateImageHash()
        {
            if(!Directory.Exists(@"C:\temp\image_tests"))
            {
                return;
            }
            var imageFilename = @"C:\temp\image_tests\1.jpg";
            var secondImagFilename = @"C:\temp\image_tests\2.jpg";
            var resizedFirstImageFilename = @"C:\temp\image_tests\1-resized.jpg";
            var thirdImageFilename = @"C:\temp\image_tests\3.jpg";

            var hash = ImageHasher.AverageHash(imageFilename);
            Assert.True(hash > 0);

            var secondHash = ImageHasher.AverageHash(imageFilename);
            Assert.True(secondHash > 0);
            Assert.Equal(hash, secondHash);

            secondHash = ImageHasher.AverageHash(secondImagFilename);
            Assert.True(secondHash > 0);
            Assert.Equal(hash, secondHash);

            var similar = ImageHasher.Similarity(imageFilename, secondImagFilename);
            Assert.Equal(100d, similar);

            Assert.True(ImageHasher.ImagesAreSame(imageFilename, secondImagFilename));


            secondHash = ImageHasher.AverageHash(resizedFirstImageFilename);
            Assert.True(secondHash > 0);
            Assert.Equal(hash, secondHash);

            Assert.False(ImageHasher.ImagesAreSame(imageFilename, thirdImageFilename));
        }
    }
}
