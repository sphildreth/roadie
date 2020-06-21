using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using configuration = Roadie.Library.Configuration;
using Roadie.Library.Inspect;
using Roadie.Library.Inspect.Plugins.File;
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
    public class InspectorTests
    {
        private IEventMessageLogger MessageLogger { get; }
        private ILogger Logger
        {
            get
            {
                return MessageLogger as ILogger;
            }
        }

        private ID3TagsHelper TagsHelper { get; }

        private configuration.IRoadieSettings Configuration { get; }
        public DictionaryCacheManager CacheManager { get; }


        public InspectorTests()
        {
            MessageLogger = new EventMessageLogger<ID3TagsHelperTests>();
            MessageLogger.Messages += MessageLoggerMessages;

            var settings = new configuration.RoadieSettings();
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.test.json");
            IConfiguration configuration = configurationBuilder.Build();
            configuration.GetSection("RoadieSettings").Bind(settings);
            settings.ConnectionString = configuration.GetConnectionString("RoadieDatabaseConnection");
            Configuration = settings;
            CacheManager = new DictionaryCacheManager(Logger, new NewtonsoftCacheSerializer(Logger), new CachePolicy(TimeSpan.FromHours(4)));
            var tagHelperLooper = new EventMessageLogger<ID3TagsHelper>();
            tagHelperLooper.Messages += MessageLoggerMessages;
            TagsHelper = new ID3TagsHelper(Configuration, CacheManager, tagHelperLooper);
        }

        private void MessageLoggerMessages(object sender, EventMessage e)
        {
            Console.WriteLine($"Log Level [{ e.Level }] Log Message [{ e.Message }] ");
        }

        [Theory]
        [InlineData("Bob Jones", "Bob Jones")]
        [InlineData("Bob Jones\\Mary Jones", "Bob Jones/Mary Jones")]
        [InlineData("Bob Jones;Mary Jones", "Bob Jones/Mary Jones")]
        [InlineData("Bob Jones; Mary Jones", "Bob Jones/Mary Jones")]
        [InlineData("Bob Jones ; Mary Jones", "Bob Jones/Mary Jones")]
        [InlineData("Bob Jones/Mary Jones", "Bob Jones/Mary Jones")]
        [InlineData("Bob Jones & Mary Jones", "Bob Jones & Mary Jones")]
        [InlineData("Bob Jones ", "Bob Jones")]
        [InlineData(" BoB   Jones   ", "Bob Jones")]
        [InlineData("Ain't NO THING", "Ain't No Thing")]        
        public void CleanArtistPlugin(string artist, string shouldBe)
        {
            var plugin = new CleanUpArtists(Configuration, CacheManager, Logger, TagsHelper);
            Assert.Equal(shouldBe, plugin.CleanArtist(artist));
        }

        [Theory]
        [InlineData("Ain't NO THING", "Ain't No Thing")]
        public void CleanTrackTitlePlugin(string title, string shouldBe)
        {
            var plugin = new CleanUpTrackTitle(Configuration, CacheManager, Logger, TagsHelper);
            var result = plugin.Process(new MetaData.Audio.AudioMetaData { Title = title});
            Assert.Equal(shouldBe, result.Data.Title);

        }

        [Fact]
        public void CleanArtistPluginRemoveArtistFromTrackArtist()
        {
            var artist  = "Bob Jones";
            var trackArtist = "Bob Jones;Mary Jones";
            var trackShouldBe = "Mary Jones";

            var plugin = new CleanUpArtists(Configuration, CacheManager, Logger, TagsHelper);
            var cleaned = plugin.CleanArtist(trackArtist, artist);
            Assert.Equal(trackShouldBe, cleaned);
        }

        [Theory]
        [InlineData("Bob Jones")]
        [InlineData("Nancy Jones")]
        public void GenerateInspectorArtistToken(string artist)
        {
            var token = Inspector.ArtistInspectorToken(new MetaData.Audio.AudioMetaData { Artist = artist });
            Assert.NotNull(token);
        }

        [Fact]
        public void ShouldGenerateSameTokenValue()
        {
            var md = new MetaData.Audio.AudioMetaData { Artist = "Omniversum Fractum", Release = "Paradigm Of The Elementals Essence" };
            var artistToken = Inspector.ArtistInspectorToken(md);
            Assert.NotNull(artistToken);

            var releaseToken = Inspector.ReleaseInspectorToken(md);
            Assert.NotNull(releaseToken);
            Assert.NotEqual(artistToken, releaseToken);

            var secondReleaseToken = Inspector.ReleaseInspectorToken(md);
            Assert.NotNull(releaseToken);
            Assert.NotEqual(artistToken, releaseToken);
            Assert.Equal(secondReleaseToken, releaseToken);
        }

        [Fact]
        public void GenerateInspectorTokensArtistAndReleaseUnique()
        {
            var md = new MetaData.Audio.AudioMetaData { Artist = "Bob Jones", Release = "Bob's First Release" };
            var artistToken = Inspector.ArtistInspectorToken(md);
            Assert.NotNull(artistToken);

            var releaseToken = Inspector.ReleaseInspectorToken(md);
            Assert.NotNull(releaseToken);

            Assert.NotEqual(artistToken, releaseToken);


        }
    }
}
