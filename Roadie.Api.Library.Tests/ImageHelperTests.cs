using Roadie.Library.FilePlugins;
using Roadie.Library.Imaging;
using System.IO;
using System.Linq;
using Xunit;

namespace Roadie.Library.Tests
{
    public class ImageHelperTests
    {
        [Theory]
        [InlineData("artist.jpeg")]
        [InlineData("artist.jpg")]
        [InlineData("artist.png")]
        [InlineData("Artist.Jpg")]
        [InlineData("Artist.JPG")]
        [InlineData("band.jpg")]
        [InlineData("group.jpg")]
        [InlineData("ARTIST.JPG")]
        [InlineData("GrOup.jpg")]
        [InlineData("aRtist.jpg")]
        public void Test_Should_Be_Artist_Images(string input)
        {
            Assert.True(ImageHelper.IsArtistImage(new FileInfo(input)));
        }

        [Theory]
        [InlineData("logo.jpeg")]
        [InlineData("logo.jpg")]
        [InlineData("logo.png")]
        [InlineData("Logo.Jpg")]
        [InlineData("artist_logo.jpg")]
        [InlineData("Artist_logo.jpg")]
        [InlineData("ARTIST_LOGO.JPG")]
        [InlineData("artist 1.jpg")]
        [InlineData("artist_01.jpg")]
        [InlineData("artist 03.jpg")]
        public void Test_Should_Be_Artist_Secondary_Images(string input)
        {
            Assert.True(ImageHelper.IsArtistSecondaryImage(new FileInfo(input)));
        }

        [Theory]
        [InlineData("cover.jpeg")]
        [InlineData("cover.jpg")]
        [InlineData("Cover.jpg")]
        [InlineData("cover.png")]
        [InlineData("Cover.Jpg")]
        [InlineData("Cover.JPG")]
        [InlineData("Cover.PNG")]
        [InlineData("CvR.Jpg")]
        [InlineData("Release.JPG")]
        [InlineData("folder.JPG")]
        [InlineData("front.jpg")]
        [InlineData("FrOnt.jpg")]
        [InlineData("Art - front.jpg")]
        [InlineData("Art - Front.jpg")]
        [InlineData("Art-Front.jpg")]
        [InlineData("Art- Front.jpg")]
        [InlineData("Art -Front.jpg")]
        [InlineData("F1.jpg")]
        [InlineData("F 1.jpg")]
        [InlineData("F-1.jpg")] 
        [InlineData("front_.jpg")]
        [InlineData("BIG.JPg")]
        [InlineData("BIG.PNG")]
        public void Test_Should_Be_Release_Images(string input)
        {
            Assert.True(ImageHelper.IsReleaseImage(new FileInfo(input)));
        }

        [Theory]
        [InlineData("cover.png")]
        [InlineData("Cover.jpg")]
        [InlineData("batman.txt")]
        [InlineData("Song.mp3")]
        [InlineData("batman.jpg")]
        [InlineData("logo.jpg")]
        [InlineData("Release.JPG")]
        [InlineData("front.jpg")]
        [InlineData("F1.jpg")]
        [InlineData("logo.jpeg")]
        [InlineData("logo.png")]
        [InlineData("Logo.Jpg")]
        [InlineData("artist_logo.jpg")]
        [InlineData("Artist_logo.jpg")]
        [InlineData("ARTIST_LOGO.JPG")]
        [InlineData("Artist - Front.jpg")]
        [InlineData("Artist Front.jpg")]
        [InlineData("artist 1.jpg")]
        [InlineData("artist_01.jpg")]
        [InlineData("artist 03.jpg")]
        public void Test_Should_Not_Be_Artist_Images(string input)
        {
            var t = ImageHelper.IsArtistImage(new FileInfo(input));
            Assert.False(t);
        }

        [Theory]
        [InlineData("artist.jpeg")]
        [InlineData("artist.jpg")]
        [InlineData("artist.png")]
        [InlineData("Artist.Jpg")]
        [InlineData("Artist.JPG")]
        [InlineData("band.jpg")]
        [InlineData("group.jpg")]
        [InlineData("ARTIST.JPG")]
        [InlineData("GrOup.jpg")]
        [InlineData("aRtist.jpg")]
        [InlineData("batman.txt")]
        [InlineData("Song.mp3")]
        [InlineData("batman.jpg")]
        [InlineData("logo.jpg")]
        [InlineData("cover 1.jpg")]
        [InlineData("cover_01.jpg")]
        [InlineData("cover 03.jpg")]
        [InlineData("Dixieland-Front1.jpg")]
        public void Test_Should_Not_Be_Release_Images(string input)
        {
            Assert.False(ImageHelper.IsReleaseImage(new FileInfo(input)));
        }


        [Theory]
        [InlineData("label.jpeg")]
        [InlineData("label.jpg")]
        [InlineData("label.png")]
        [InlineData("Label.Jpg")]
        [InlineData("label.JPG")]
        [InlineData("record_label.jpg")]
        [InlineData("RecordLabel.jpg")]
        [InlineData("RECORDLABEL.JPG")]
        public void Test_Should_Be_Label_Images(string input)
        {
            Assert.True(ImageHelper.IsLabelImage(new FileInfo(input)));
        }

        [Theory]
        [InlineData("artist.jpeg")]
        [InlineData("artist.jpg")]
        [InlineData("artist.png")]
        [InlineData("Artist.Jpg")]
        [InlineData("Artist.JPG")]
        [InlineData("band.jpg")]
        [InlineData("group.jpg")]
        [InlineData("ARTIST.JPG")]
        [InlineData("GrOup.jpg")]
        [InlineData("aRtist.jpg")]
        [InlineData("cover.jpeg")]
        [InlineData("cover.jpg")]
        [InlineData("cover.png")]
        [InlineData("Cover.Jpg")]
        [InlineData("Release.JPG")]
        [InlineData("front.jpg")]
        [InlineData("FrOnt.jpg")]
        public void Test_Should_NotBe_Label_Images(string input)
        {
            Assert.False(ImageHelper.IsLabelImage(new FileInfo(input)));
        }

        [Theory]

        [InlineData("Booklet-1.jpg")]
        [InlineData("Booklet-10.jpg")]
        [InlineData("Booklet_1.jpg")] 
        [InlineData("Booklet 3.jpg")] 
        [InlineData("Booklet.jpg")]
        [InlineData("Book.jpg")]
        [InlineData("Book_3.jpg")]
        [InlineData("Book_03.jpg")]
        [InlineData("Book-1.jpg")]
        [InlineData("Book-01.jpg")]
        [InlineData("Back.jpg")]
        [InlineData("BAcK.JPg")]
        [InlineData("Cd.jpg")]
        [InlineData("CD.JPG")] 
        [InlineData("Cd1.jpg")]
        [InlineData("CD-1.jpg")]
        [InlineData("CD 1.jpg")]
        [InlineData("CD_1.jpg")]
        [InlineData("CD-5.jpg")]
        [InlineData("CD3.jpg")]
        [InlineData("Scan-1.jpg")]
        [InlineData("Scan-12.jpg")]
        [InlineData("cover_01.jpg")]
        [InlineData("cover 03.jpg")]
        [InlineData("cover 1.jpg")]
        [InlineData("Encartes (11).jpg")]
        [InlineData("Encartes (1).png")]
        [InlineData("Encartes.jpg")]
        [InlineData("Art - Back.jpg")]
        [InlineData("disc.jpg")]
        [InlineData("disc.png")]
        [InlineData("inside.jpg")]
        [InlineData("Inside1.jpg")]
        [InlineData("Inside 1.jpg")]
        [InlineData("Inside-1.jpg")]
        [InlineData("in1.jpg")]
        [InlineData("inlay.jpg")]
        [InlineData("release 1.jpg")]
        [InlineData("release-1.jpg")]
        [InlineData("release_1.jpg")]
        [InlineData("release 3.jpg")]
        [InlineData("release 10.jpg")]
        [InlineData("Dixieland-Label-Side 1.JPG")]
        [InlineData("Dixieland-Label-Side 2.JPG")] 
        [InlineData("Hearing Is Believing-Inside 1.jpg")]
        [InlineData("Booklet (2-3).jpg")] 
        [InlineData("Booklet (14-15).jpg")] 
        [InlineData("Booklet#2.jpg")] 
        [InlineData("traycard.png")] 
        [InlineData("Jewel Case.jpg")]
        [InlineData("Matrix-1.jpg")]
        [InlineData("Matrix 1.jpg")]
        public void Test_Should_Be_Release_Secondary_Images(string input)
        {
            Assert.True(ImageHelper.IsReleaseSecondaryImage(new FileInfo(input)));
        }

        [Theory]
        [InlineData("artist.jpeg")]
        [InlineData("artist.jpg")]
        [InlineData("artist.png")]
        [InlineData("Artist.Jpg")]
        [InlineData("Artist.JPG")]
        [InlineData("band.jpg")]
        [InlineData("group.jpg")]
        [InlineData("ARTIST.JPG")]
        [InlineData("GrOup.jpg")]
        [InlineData("aRtist.jpg")]
        [InlineData("cover.jpeg")]
        [InlineData("cover.jpg")]
        [InlineData("cover.png")]
        [InlineData("Cover.Jpg")]
        [InlineData("Release.JPG")]
        [InlineData("front.jpg")]
        [InlineData("FrOnt.jpg")]
        [InlineData("label.jpeg")]
        [InlineData("label.jpg")]
        [InlineData("label.png")]
        [InlineData("Label.Jpg")]
        [InlineData("label.JPG")]
        [InlineData("record_label.jpg")]
        [InlineData("RecordLabel.jpg")]
        [InlineData("RECORDLABEL.JPG")]
        public void Test_Should_Not_Be_Release_Secondary_Images(string input)
        {
            Assert.False(ImageHelper.IsReleaseSecondaryImage(new FileInfo(input)));
        }

        [Fact]
        public void Get_Release_Image_In_Folder()
        {
            var folder = new DirectoryInfo(@"C:\roadie_dev_root\image_tests");
            if(!folder.Exists)
            {
                Assert.True(true);
                return;
            }
            var cover = ImageHelper.FindImageTypeInDirectory(folder, Enums.ImageType.Release);
            Assert.NotNull(cover);

            var secondaryImages = ImageHelper.FindImageTypeInDirectory(folder, Enums.ImageType.ReleaseSecondary, SearchOption.AllDirectories);
            Assert.NotNull(secondaryImages);
        }

        [Fact]
        public void Get_Artist_Image_In_Folder()
        {
            var folder = new DirectoryInfo(@"C:\roadie_dev_root\image_tests)");
            if (!folder.Exists)
            {
                Assert.True(true);
                return;
            }
            var artist = ImageHelper.FindImageTypeInDirectory(folder, Enums.ImageType.Artist);
            Assert.NotNull(artist);
            Assert.Single(artist);
            Assert.Equal("artist.jpg", artist.First().Name);
        }
    }
}