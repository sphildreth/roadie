using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.Processors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Roadie.Library.Tests
{
    public class ID3TagsHelperTests
    {
        private IEventMessageLogger MessageLogger { get; }
        private ILogger Logger
        {
            get
            {
                return this.MessageLogger as ILogger;
            }
        }

        private ID3TagsHelper TagsHelper { get; }

        private IRoadieSettings Configuration { get; }
        public DictionaryCacheManager CacheManager { get; }

        public ID3TagsHelperTests()
        {
            this.MessageLogger = new EventMessageLogger();
            this.MessageLogger.Messages += MessageLogger_Messages;

            var settings = new RoadieSettings();
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.test.json");
            IConfiguration configuration = configurationBuilder.Build();
            configuration.GetSection("RoadieSettings").Bind(settings);
            settings.ConnectionString = configuration.GetConnectionString("RoadieDatabaseConnection");
            this.Configuration = settings;
            this.CacheManager = new DictionaryCacheManager(this.Logger, new CachePolicy(TimeSpan.FromHours(4)));
            this.TagsHelper = new ID3TagsHelper(this.Configuration, this.CacheManager, this.Logger);
        }

        private void MessageLogger_Messages(object sender, EventMessage e)
        {
            Console.WriteLine($"Log Level [{ e.Level }] Log Message [{ e.Message }] ");
        }

        [Theory]
        [InlineData("1")]
        [InlineData("01")]
        [InlineData("1/2")]
        [InlineData("A1")]
        [InlineData("A1/2")]
        public void ParseTrackNumberShouldBeOne(string trackNumber)
        {
            var dn = ID3TagsHelper.ParseTrackNumber(trackNumber);
            Assert.NotNull(dn);
            Assert.Equal(1, dn.Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Z")]
        [InlineData("-----")]
        [InlineData("?")]
        public void ParseTrackNumberShouldBeNull(string trackNumber)
        {
            var dn = ID3TagsHelper.ParseTrackNumber(trackNumber);
            Assert.Null(dn);
        }

        [Theory]
        [InlineData("2/5")]
        [InlineData("02/05")]
        [InlineData("A/5")]
        [InlineData("B2/B5")]
        public void ParseTotalTrackNumberShouldBeFive(string trackNumber)
        {
            var dn = ID3TagsHelper.ParseTotalTrackNumber(trackNumber);
            Assert.NotNull(dn);
            Assert.Equal(5, dn.Value);
        }


        [Theory]
        [InlineData("1983")]
        [InlineData("02/24/1983")]
        [InlineData("1983/02/24")]
        [InlineData("1983//1983")]
        [InlineData("1983\\1983")]
        [InlineData("1983,1983")]
        [InlineData("1983;1983")]
        [InlineData("1983 1983")]
        [InlineData("1983;;1983")]
        [InlineData("1983; 1983")]
        [InlineData("83")]
        public void ParseYearShouldBeNinteenEightyThree(string year)
        {
            var dn = ID3TagsHelper.ParseYear(year);
            Assert.NotNull(dn);
            Assert.Equal((short)1983, dn);
        }

        [Theory]
        [InlineData("")]
        [InlineData("-")]
        [InlineData("199983")]
        [InlineData("19983")]
        [InlineData("321")]
        public void ParseYearShouldBeBad(string year)
        {
            var dn = ID3TagsHelper.ParseYear(year);
            Assert.Null(dn);
        }


        [Theory]
        [InlineData("")]
        [InlineData("Z")]
        [InlineData("-----")]
        [InlineData("?")]
        [InlineData("99")]
        public void ParseTotalTrackNumberShouldBeNull(string trackNumber)
        {
            var dn = ID3TagsHelper.ParseTotalTrackNumber(trackNumber);
            Assert.Null(dn);
        }


        [Theory]
        [InlineData("2")]
        [InlineData("02")]
        [InlineData("2/2")]
        [InlineData("B2")]
        [InlineData("B2/2")]
        public void ParseTrackNumberShouldBeTwo(string trackNumber)
        {
            var dn = ID3TagsHelper.ParseTrackNumber(trackNumber);
            Assert.NotNull(dn);
            Assert.Equal(2, dn.Value);
        }


        [Theory]
        [InlineData("1")]
        [InlineData("01")]
        [InlineData("001")]
        [InlineData("1/2")]
        [InlineData("01/02")]
        [InlineData("001/002")]
        [InlineData("3/9")]
        [InlineData(@"2\2")]
        [InlineData("2:2")]
        [InlineData("2,2")]
        [InlineData("2|2")]
        [InlineData("99/100")]
        [InlineData("33")]
        [InlineData("A")]
        [InlineData("B")]
        [InlineData("A/2")]
        [InlineData("A/B")]
        public void ParseDiscNumberGood(string discNumber)
        {
            var dn = ID3TagsHelper.ParseDiscNumber(discNumber);
            Assert.NotNull(dn);
            Assert.True(dn > 0);
        }

        [Theory]
        [InlineData("")]        
        [InlineData(" /2")]
        [InlineData(null)]
        [InlineData("BATMAN")]
        [InlineData(".")]
        [InlineData("#")]
        public void ParseDiscNumberBad(string discNumber)
        {
            var dn = ID3TagsHelper.ParseDiscNumber(discNumber);
            Assert.Null(dn);
        }

        [Theory]
        [InlineData("01. Up.mp3")]
        [InlineData("01 - Relentless Ascension.mp3")]
        [InlineData("01.Say Goodbye.mp3")]
        [InlineData("01 Up.mp3")]
        [InlineData("1 - Tithe II.mp3")]
        [InlineData("1 Up.mp3")]
        [InlineData("1. Up.mp3")]
        [InlineData("1- Up.mp3")] 
        [InlineData("(01) [Iperyt] The Black Emperor.mp3")]
        [InlineData("[01] [Iperyt] The Black Emperor.mp3")] 
        [InlineData("Van Morrison - 01 - Some Peace of Mind (with Bobby Womack).mp3")] 
        public void DetermineTrackNumber(string filename)
        {
            var tn = ID3TagsHelper.DetermineTrackNumber(filename);
            Assert.True(tn > 0);
        }

        [Fact]
        public void ReadTotalTrackNumbersFromCue_Should_Be_Five()
        {
            var cuesDir = @"C:\roadie_dev_root\test_cue_and_playlists\cues1";
            var directory = new DirectoryInfo(cuesDir);
            if(directory.Exists)
            { 
                foreach(var file in Directory.GetFiles(cuesDir))
                {
                    var t = ID3TagsHelper.DetermineTotalTrackNumbers(file);
                    Assert.Equal(5, t.Value);
                }    
            }
            else
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadTotalTrackNumbersFromCue_Should_Be_Eight()
        {
            var cuesDir = @"C:\roadie_dev_root\test_cue_and_playlists\cues2";
            var directory = new DirectoryInfo(cuesDir);
            if (directory.Exists)
            {
                foreach (var file in Directory.GetFiles(cuesDir))
                {
                    var t = ID3TagsHelper.DetermineTotalTrackNumbers(file);
                    Assert.Equal(8, t.Value);
                }
            }
            else
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadTotalTrackNumbersFromCue_Should_Be_Six()
        {
            var cuesDir = @"C:\roadie_dev_root\test_cue_and_playlists\cues3";
            var directory = new DirectoryInfo(cuesDir);
            if (directory.Exists)
            {
                foreach (var file in Directory.GetFiles(cuesDir))
                {
                    var t = ID3TagsHelper.DetermineTotalTrackNumbers(file);
                    Assert.Equal(6, t.Value);
                }
            }
            else
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadTotalTrackNumbersFromCue_Should_Be_Nine()
        {
            var cuesDir = @"C:\roadie_dev_root\test_cue_and_playlists\cues4";
            var directory = new DirectoryInfo(cuesDir);
            if (directory.Exists)
            {
                foreach (var file in Directory.GetFiles(cuesDir))
                {
                    var t = ID3TagsHelper.DetermineTotalTrackNumbers(file);
                    Assert.Equal(9, t.Value);
                }
            }
            else
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadTotalTrackNumbersFromM3u_Should_Be_Eleven()
        {
            var cuesDir = @"C:\roadie_dev_root\test_cue_and_playlists\m3u1";
            var directory = new DirectoryInfo(cuesDir);
            if (directory.Exists)
            {
                foreach (var file in Directory.GetFiles(cuesDir))
                {
                    var t = ID3TagsHelper.DetermineTotalTrackNumbers(file);
                    Assert.Equal(11, t.Value);
                }
            }
            else
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadTotalTrackNumbersFromM3u_Should_Be_Four()
        {
            var cuesDir = @"C:\roadie_dev_root\test_cue_and_playlists\m3u2";
            var directory = new DirectoryInfo(cuesDir);
            if (directory.Exists)
            {
                foreach (var file in Directory.GetFiles(cuesDir))
                {
                    var t = ID3TagsHelper.DetermineTotalTrackNumbers(file);
                    Assert.Equal(4, t.Value);
                }
            }
            else
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadTotalTrackNumbersFromM3u_Should_Be_Eight()
        {
            var cuesDir = @"C:\roadie_dev_root\test_cue_and_playlists\m3u3";
            var directory = new DirectoryInfo(cuesDir);
            if (directory.Exists)
            {
                foreach (var file in Directory.GetFiles(cuesDir))
                {
                    var t = ID3TagsHelper.DetermineTotalTrackNumbers(file);
                    Assert.Equal(8, t.Value);
                }
            }
            else
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadTotalTrackNumbersFromM3u_Should_Be_Fourteen()
        {
            var cuesDir = @"C:\roadie_dev_root\test_cue_and_playlists\m3u4";
            var directory = new DirectoryInfo(cuesDir);
            if (directory.Exists)
            {
                foreach (var file in Directory.GetFiles(cuesDir))
                {
                    var t = ID3TagsHelper.DetermineTotalTrackNumbers(file);
                    Assert.Equal(14, t.Value);
                }
            }
            else
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadID3v24Tags()
        {
      //      var file = new FileInfo(@"N:\_complete\JN Dutplanet net Reset String - Ancient Future 2014\JN Dutplanet.net Reset String - Ancient Future 2014\01 O.P.D. (Obsessive Personality Disorder).mp3");
            var file = new FileInfo(@"C:\roadie_dev_root\mp3_tests\01 O.P.D. (Obsessive Personality Disorder).mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName, true);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }


        [Fact]
        public void ReadID3TagsMultipleMediasWithMax()
        {
            var file = new FileInfo(@"C:\roadie_dev_root\library\Dream Theater\[2016] The Astonishing\01 2285 Entr acte.mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.Equal(2, metaData.Disc);
                Assert.NotNull(metaData.TrackNumber);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadID3TagsFromFileWithTrackArtists()
        {
            var file = new FileInfo(@"E:\Roadie_Test_Files\13-anna_kendrick-true_colors-a57e270d\01-justin_timberlake-hair_up-ef53c026.mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.TrackArtist);
                Assert.NotEqual(metaData.Artist, metaData.TrackArtist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Theory]
        [InlineData(@"C:\roadie_dev_root\Rita Ora - Phoenix (Deluxe) (2018) Mp3 (320kbps) [Hunter]\Rita Ora - Phoenix (Deluxe) (2018)")]
        [InlineData(@"C:\roadie_dev_root\Travis Scott - ASTROWORLD (2018) Mp3 (320kbps) [Hunter]")]
        [InlineData(@"C:\roadie_dev_root\Lil Wayne - Tha Carter V (2018) Mp3 (320kbps) [Hunter]")]
        [InlineData(@"C:\roadie_dev_root\Beyonce & JAY-Z - EVERYTHING IS LOVE (2018) Mp3 (320kbps) [Hunter]")]
        public void ReadFolderTestAllFiles(string folderName)
        {
            if (!Directory.Exists(folderName))
            {
                Assert.True(true);
            }
            else
            {
                var folderFiles = Directory.GetFiles(folderName, "*.mp3", SearchOption.AllDirectories);
                foreach(var file in folderFiles)
                {
                    var tagLib = this.TagsHelper.MetaDataForFile(file);
                    Assert.True(tagLib.IsSuccess);
                    var metaData = tagLib.Data;
                    Assert.NotNull(metaData.Artist);
                    Assert.NotNull(metaData.Release);
                    Assert.NotNull(metaData.Title);
                    Assert.True(metaData.Year > 0);
                    Assert.True(metaData.ValidWeight > 30);
                    Assert.True(metaData.IsValid);
                }
            }

        }

        [Fact]
        public void ReadID3TagsFromFileSoundtrackInTitleNotSoundtrack()
        {
            var file = new FileInfo(@"E:\Roadie_Test_Files\01 01 Angel Of Death.mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.False(metaData.IsSoundTrack);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadID3TagsFromFileIsSoundtrack()
        {
            var file = new FileInfo(@"E:\Roadie_Test_Files\06 You'Re Sensational.mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.IsSoundTrack);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadID3TagsFromFileWithAlbumNoTrackSet()
        {
            var file = new FileInfo(@"M:\inbound\MEGAPACK ---METAL-DEATH-BLACK---\ebony_tears-evil_as_hell-2001-ss\01-deviation-ss.mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.TrackArtist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void Read_File_Test_Is_valid()
        {
            var file = new FileInfo(@"M:\unknown\2eec19bd-3575-4b7f-84dd-db2a0ec3e2f3~[2009] Dolly - Disc 1 Of 4~06 Nobody But You (Previously Unissued).mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.Null(metaData.TrackArtist);
                Assert.False(metaData.TrackArtists.Any());
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void Read_File_Test_Is_valid2()
        {
            var file = new FileInfo(@"M:\library_old\Perverse\[2014] Champion Dub\01 Champion Dub (Original Mix).mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.Null(metaData.TrackArtist);
                Assert.False(metaData.TrackArtists.Any());
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void Read_File_Test_Is_valid3()
        {
            var file = new FileInfo(@"C:\roadie_dev_root\inbound\Dreadful Fate - Vengeance (2018)\01-dreadful_fate-vengeance.mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.Null(metaData.TrackArtist);
                Assert.False(metaData.TrackArtists.Any());
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }


        [Fact]
        public void ReadID3TagsFromFileWithTrackAndArtistTheSame()
        {
            var file = new FileInfo(@"M:\library\Blind Melon\[1992] Blind Melon\01. Blind Melon - Soak The Sin.mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.Null(metaData.TrackArtist);
                Assert.False(metaData.TrackArtists.Any());
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadID3TagsFromFileWithArtistAndTrackArtist()
        {
            var file = new FileInfo(@"E:\Roadie_Test_Files\Test.mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.TrackArtist);
                Assert.Equal("Da Album Artist", metaData.Artist);
                Assert.Equal("Da Artist", metaData.TrackArtist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.NotNull(metaData.Genres);
                Assert.NotNull(metaData.Genres.First());
                Assert.Equal(2011, metaData.Year);
                Assert.NotNull(metaData.TrackNumber);
                Assert.Equal(6, metaData.TrackNumber.Value);
                Assert.Equal(64, metaData.TotalTrackNumbers);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void Write_Tags()
        {
            var file = new FileInfo(@"C:\roadie_dev_root\inbound\temp\01. Re1nstall 0verture.mp3");
            if (file.Exists)
            {
                short trackNumber = 15;
                var numberOfTracks = 25;

                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                metaData.TrackNumber = trackNumber;
                metaData.TotalTrackNumbers = numberOfTracks;
                this.TagsHelper.WriteTags(metaData, file.FullName);

                var tagLibAfterWrite = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                Assert.Equal(metaData.Artist, tagLibAfterWrite.Data.Artist);
                Assert.Equal(metaData.Release, tagLibAfterWrite.Data.Release);
                Assert.Equal(metaData.Title, tagLibAfterWrite.Data.Title);
                Assert.Equal(trackNumber, tagLibAfterWrite.Data.TrackNumber);
                Assert.Equal(numberOfTracks, tagLibAfterWrite.Data.TotalTrackNumbers);
                


            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadID3TagsFromFile2()
        {
            var file = new FileInfo(@"M:\library\Denver, John\[1972] Aerie\10 Readjustment Blues.mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.True(metaData.IsValid);
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.Equal(10, metaData.TrackNumber.Value);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadID3TagsFromFile3()
        {
            var file = new FileInfo(@"E:\Roadie_Test_Files\01. What's Yesterday.mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.Equal(1, metaData.TrackNumber.Value);
                Assert.Equal(10, metaData.TotalTrackNumbers.Value);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadID3TagsFromFile4()
        {
            var file = new FileInfo(@"M:\library\Ac Dc\[1975] T.N.T\01 It'S A Long Way To The Top (If You Wanna Rock 'N' Roll).mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.Equal(10, metaData.TrackNumber.Value);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadID3TagsFromFile5()
        {
            var file = new FileInfo(@"E:\Roadie_Test_Files\01 Martian.mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.Equal(1, metaData.TrackNumber.Value);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadID3TagsFromFile6()
        {
            var file = new FileInfo(@"C:\roadie_dev_root\inbound\[2016] Invention Of Knowledge\01 Invention.mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.Equal(1, metaData.TrackNumber.Value);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadID3TagsFromFile7()
        {
            var file = new FileInfo(@"C:\roadie_dev_root\1985 - Sacred Heart\Dio - Sacred Heart (1).mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.Equal(1, metaData.TrackNumber.Value);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadID3TagsFromFile8()
        {
            var file = new FileInfo(@"C:\roadie_dev_root\Grift\2017 Arvet\01 - Flyktfast.mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.Equal(1, metaData.TrackNumber.Value);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Fact]
        public void ReadID3TagsFromFile9()
        {
            var file = new FileInfo(@"C:\roadie_dev_root\Distorted Harmony -  A Way Out - 2018\kWlZr0N_o72dwo0_CD001_0001.mp3");
            if (file.Exists)
            {
                var tagLib = this.TagsHelper.MetaDataForFile(file.FullName);
                Assert.True(tagLib.IsSuccess);
                var metaData = tagLib.Data;
                Assert.NotNull(metaData.Artist);
                Assert.NotNull(metaData.Release);
                Assert.NotNull(metaData.Title);
                Assert.True(metaData.Year > 0);
                Assert.NotNull(metaData.TrackNumber);
                Assert.Equal(1, metaData.TrackNumber.Value);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
                Assert.True(metaData.IsValid);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

    }
}
