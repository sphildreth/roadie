﻿using Microsoft.Extensions.Configuration;
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
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }

        [Theory]
        [InlineData(@"N:\Rita Ora - Phoenix (Deluxe) (2018) Mp3 (320kbps) [Hunter]\Rita Ora - Phoenix (Deluxe) (2018)")]
        [InlineData(@"N:\Travis Scott - ASTROWORLD (2018) Mp3 (320kbps) [Hunter]")]
        [InlineData(@"N:\Lil Wayne - Tha Carter V (2018) Mp3 (320kbps) [Hunter]")]
        [InlineData(@"N:\Beyonce & JAY-Z - EVERYTHING IS LOVE (2018) Mp3 (320kbps) [Hunter]")]
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
            var file = new FileInfo(@"Z:\inbound\MEGAPACK ---METAL-DEATH-BLACK---\ebony_tears-evil_as_hell-2001-ss\01-deviation-ss.mp3");
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
            var file = new FileInfo(@"Z:\unknown\2eec19bd-3575-4b7f-84dd-db2a0ec3e2f3~[2009] Dolly - Disc 1 Of 4~06 Nobody But You (Previously Unissued).mp3");
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
            var file = new FileInfo(@"Z:\library_old\Perverse\[2014] Champion Dub\01 Champion Dub (Original Mix).mp3");
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
            var file = new FileInfo(@"Z:\library\Blind Melon\[1992] Blind Melon\01. Blind Melon - Soak The Sin.mp3");
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
        public void ReadID3TagsFromFile2()
        {
            var file = new FileInfo(@"Z:\library\Denver, John\[1972] Aerie\10 Readjustment Blues.mp3");
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
                Assert.Equal(metaData.TrackNumber.Value, 10);
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
            var file = new FileInfo(@"Z:\library\Ac Dc\[1975] T.N.T\01 It'S A Long Way To The Top (If You Wanna Rock 'N' Roll).mp3");
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
                Assert.Equal(metaData.TrackNumber.Value, 10);
                Assert.True(metaData.TotalSeconds > 0);
                Assert.True(metaData.ValidWeight > 30);
            }
            else
            {
                Console.WriteLine($"skipping { file}");
                Assert.True(true);
            }
        }


    }
}