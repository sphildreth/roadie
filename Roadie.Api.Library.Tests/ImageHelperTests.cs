using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.FilePlugins;
using Roadie.Library.Imaging;
using System;
using System.Diagnostics;
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
        [InlineData("photo.jpg")]
        [InlineData("aRtist.jpg")]
        public void TestShouldBeArtistImages(string input)
        {
            Assert.True(ImageHelper.IsArtistImage(new FileInfo(input)));
        }

        [Theory]
        [InlineData("logo.jpeg")]
        [InlineData("logo.jpg")]
        [InlineData("logo.png")]
        [InlineData("Logo.Jpg")]
        [InlineData("logo.gif")]
        [InlineData("artist_logo.jpg")]
        [InlineData("Artist_logo.jpg")]
        [InlineData("ARTIST_LOGO.JPG")]
        [InlineData("artist 1.jpg")]
        [InlineData("artist_01.jpg")]
        [InlineData("artist 03.jpg")]
        [InlineData("band 01.jpg")]
        [InlineData("band_01.jpg")]
        [InlineData("band 1.jpg")]
        [InlineData("photo 1.jpg")]
        [InlineData("photo1.jpg")]
        public void TestShouldBeArtistSecondaryImages(string input)
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
        [InlineData("Art.jpg")]
        [InlineData("Art - front.jpg")]
        [InlineData("Art - Front.jpg")]
        [InlineData("Art-Front.jpg")]
        [InlineData("Art- Front.jpg")]
        [InlineData("Art -Front.jpg")]
        [InlineData("f.jpg")]
        [InlineData("F1.jpg")]
        [InlineData("F 1.jpg")]
        [InlineData("F-1.jpg")] 
        [InlineData("front_.jpg")]
        [InlineData("BIG.JPg")]
        [InlineData("bigart.JPg")]
        [InlineData("BIG.PNG")]
        public void TestShouldBeReleaseImages(string input)
        {
            Assert.True(ImageHelper.IsReleaseImage(new FileInfo(input)));
        }

        [Theory]
        [InlineData("cover.png")]
        [InlineData("cover1.jpg")]
        [InlineData("cover 1.jpg")]
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
        public void TestShouldNotBeArtistImages(string input)
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
        public void TestShouldNotBeReleaseImages(string input)
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
        public void TestShouldBeLabelImages(string input)
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
        public void TestShouldNotBeLabelImages(string input)
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
        [InlineData("Book 3.jpg")]
        [InlineData("Book 99.jpg")]
        [InlineData("book 99.jpg")]
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
        [InlineData("Scan 1.jpg")]
        [InlineData("sc 1.jpg")]
        [InlineData("sc01.jpg")]
        [InlineData("sc-01.jpg")]
        [InlineData("sc 01.jpg")]
        [InlineData("cover_01.jpg")]
        [InlineData("cover 03.jpg")]
        [InlineData("cover 1.jpg")]
        [InlineData("cover1.jpg")]
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
        [InlineData("IMG_20160921_0004.jpg")] 
        public void TestShouldBeReleaseSecondaryImages(string input)
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
        public void TestShouldNotBeReleaseSecondaryImages(string input)
        {
            Assert.False(ImageHelper.IsReleaseSecondaryImage(new FileInfo(input)));
        }

        [Fact]
        public void GetReleaseImageInFolder()
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
        public void GetArtistImageInFolder()
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

        [Fact]
        public void ExtractImagesFromDatabase()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var now = DateTime.UtcNow;
            var optionsBuilder = new DbContextOptionsBuilder<RoadieDbContext>();
            optionsBuilder.UseMySql("server=viking;userid=roadie;password=MenAtW0rk668;persistsecurityinfo=True;database=roadie_dev;ConvertZeroDateTime=true");

            var settings = new RoadieSettings();
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.test.json");
            IConfiguration configuration = configurationBuilder.Build();
            configuration.GetSection("RoadieSettings").Bind(settings);
            settings.ConnectionString = configuration.GetConnectionString("RoadieDatabaseConnection");            

            using (var context = new RoadieDbContext(optionsBuilder.Options))
            {
                foreach (var artist in context.Artists.Where(x => x.Thumbnail != null).OrderBy(x => x.SortName ?? x.Name))
                {
                    var artistFolder = artist.ArtistFileFolder(settings);
                    if (!Directory.Exists(artistFolder))
                    {
                        Directory.CreateDirectory(artistFolder);
                    }
                    var artistImage = Path.Combine(artistFolder, ImageHelper.ArtistImageFilename);
                    if (!File.Exists(artistImage))
                    {
                        File.WriteAllBytes(artistImage, ImageHelper.ConvertToJpegFormat(artist.Thumbnail));
                    }
                    artist.Thumbnail = null;
                    artist.LastUpdated = now;
                    Trace.WriteLine($"Saved Artist Image `{artist}` path [{ artistImage }]");
                }
                context.SaveChanges();

                var artistImages = (from i in context.Images
                                    join a in context.Artists on i.ArtistId equals a.Id
                                    select new { i, a});
                foreach(var artistImage in artistImages)
                {
                    var looper = 0;
                    var artistFolder = artistImage.a.ArtistFileFolder(settings);
                    var artistImageFilename = Path.Combine(artistFolder, string.Format(ImageHelper.ArtistSecondaryImageFilename, looper.ToString("00")));
                    while (File.Exists(artistImageFilename))
                    {
                        looper++;
                        artistImageFilename = Path.Combine(artistFolder, string.Format(ImageHelper.ArtistSecondaryImageFilename, looper.ToString("00")));
                    }
                    File.WriteAllBytes(artistImageFilename, ImageHelper.ConvertToJpegFormat(artistImage.i.Bytes));
                    context.Images.Remove(artistImage.i);
                    Trace.WriteLine($"Saved Artist Secondary Image `{artistImage.a}` path [{ artistImageFilename }]");
                }
                context.SaveChanges();

                foreach (var collection in context.Collections.Where(x => x.Thumbnail != null).OrderBy(x => x.SortName ?? x.Name))
                {
                    var image = collection.PathToImage(settings);
                    if (!File.Exists(image))
                    {
                        File.WriteAllBytes(image, ImageHelper.ConvertToJpegFormat(collection.Thumbnail));
                    }
                    collection.Thumbnail = null;
                    collection.LastUpdated = now;
                    Trace.WriteLine($"Saved Collection Image `{collection}` path [{ image }]");
                }
                context.SaveChanges();

                foreach (var genre in context.Genres.Where(x => x.Thumbnail != null).OrderBy(x => x.Name))
                {
                    var image = genre.PathToImage(settings);
                    if (!File.Exists(image))
                    {
                        File.WriteAllBytes(image, ImageHelper.ConvertToJpegFormat(genre.Thumbnail));
                    }
                    genre.Thumbnail = null;
                    genre.LastUpdated = now;
                    Trace.WriteLine($"Saved Genre Image `{genre}` path [{ image }]");
                }
                context.SaveChanges();

                foreach (var label in context.Labels.Where(x => x.Thumbnail != null).OrderBy(x => x.SortName ?? x.Name))
                {
                    var image = label.PathToImage(settings);
                    if (!File.Exists(image))
                    {
                        File.WriteAllBytes(image, ImageHelper.ConvertToJpegFormat(label.Thumbnail));
                    }
                    label.Thumbnail = null;
                    label.LastUpdated = now;
                    Trace.WriteLine($"Saved Label Image `{label}` path [{ image }]");
                }
                context.SaveChanges();

                foreach (var playlist in context.Playlists.Where(x => x.Thumbnail != null).OrderBy(x => x.Name))
                {
                    var image = playlist.PathToImage(settings);
                    if (!File.Exists(image))
                    {
                        File.WriteAllBytes(image, ImageHelper.ConvertToJpegFormat(playlist.Thumbnail));
                    }
                    playlist.Thumbnail = null;
                    playlist.LastUpdated = now;
                    Trace.WriteLine($"Saved Playlist Image `{playlist}` path [{ image }]");
                }
                context.SaveChanges();

                foreach (var release in context.Releases.Include(x => x.Artist).Where(x => x.Thumbnail != null).OrderBy(x => x.Title))
                {
                    var artistFolder = release.Artist.ArtistFileFolder(settings);
                    var releaseFolder = release.ReleaseFileFolder(artistFolder);
                    if (!Directory.Exists(releaseFolder))
                    {
                        Directory.CreateDirectory(artistFolder);
                    }
                    var releaseImage = Path.Combine(releaseFolder, "cover.jpg");
                    if (!File.Exists(releaseImage))
                    {
                        File.WriteAllBytes(releaseImage, ImageHelper.ConvertToJpegFormat(release.Thumbnail));
                    }
                    release.Thumbnail = null;
                    release.LastUpdated = now;
                    Trace.WriteLine($"Saved Release Image `{release}` path [{ releaseImage }]");
                }
                context.SaveChanges();

                var releaseImages = (from i in context.Images
                                     join r in context.Releases.Include(x => x.Artist) on i.ReleaseId equals r.Id
                                     select new { i, r });
                foreach (var releaseImage in releaseImages)
                {
                    var looper = 0;
                    var artistFolder = releaseImage.r.Artist.ArtistFileFolder(settings);
                    var releaseFolder = releaseImage.r.ReleaseFileFolder(artistFolder);
                    var releaseImageFilename = Path.Combine(artistFolder, string.Format(ImageHelper.ReleaseSecondaryImageFilename, looper.ToString("00")));
                    while (File.Exists(releaseImageFilename))
                    {
                        looper++;
                        releaseImageFilename = Path.Combine(artistFolder, string.Format(ImageHelper.ReleaseSecondaryImageFilename, looper.ToString("00")));
                    }
                    File.WriteAllBytes(releaseImageFilename, ImageHelper.ConvertToJpegFormat(releaseImage.i.Bytes));
                    context.Images.Remove(releaseImage.i);
                    Trace.WriteLine($"Saved Release Secondary Image `{releaseImage.r}` path [{ releaseImageFilename }]");
                }
                context.SaveChanges();


                foreach (var track in context.Tracks.Include(x => x.ReleaseMedia)
                                                    .Include(x => x.ReleaseMedia.Release)
                                                    .Include(x => x.ReleaseMedia.Release.Artist)
                                                    .Where(x => x.Thumbnail != null).OrderBy(x => x.Title))
                {
                    var artistFolder = track.ReleaseMedia.Release.Artist.ArtistFileFolder(settings);
                    if (!Directory.Exists(artistFolder))
                    {
                        Directory.CreateDirectory(artistFolder);
                    }
                    var releaseFolder = track.ReleaseMedia.Release.ReleaseFileFolder(artistFolder);
                    if (!Directory.Exists(releaseFolder))
                    {
                        Directory.CreateDirectory(releaseFolder);
                    }
                    var trackImage = track.PathToTrackThumbnail(settings);
                    if (!File.Exists(trackImage))
                    {
                        File.WriteAllBytes(trackImage, ImageHelper.ConvertToJpegFormat(track.Thumbnail));
                    }
                    track.Thumbnail = null;
                    track.LastUpdated = now;
                    Trace.WriteLine($"Saved Track Image `{track}` path [{ trackImage }]");
                }
                context.SaveChanges();

                foreach (var user in context.Users.Where(x => x.Avatar != null).OrderBy(x => x.UserName))
                {
                    var image = user.PathToImage(settings);
                    if (!File.Exists(image))
                    {
                        File.WriteAllBytes(image, ImageHelper.ConvertToJpegFormat(user.Avatar));
                    }
                    user.Avatar = null;
                    user.LastUpdated = now;
                    Trace.WriteLine($"Saved User Image `{user}` path [{ image }]");
                }
                context.SaveChanges();


            }
#pragma warning restore CS0618 // Type or member is obsolete

        }
    }
}