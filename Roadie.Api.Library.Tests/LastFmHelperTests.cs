using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData.LastFm;
using Roadie.Library.Processors;
using Roadie.Library.MetaData.MusicBrainz;
using Roadie.Library.SearchEngines.MetaData.Discogs;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Roadie.Library.Data.Context;
using System.Collections.Generic;

namespace Roadie.Library.Tests
{
    public class LastFmHelperTests : HttpClientFactoryBaseTests
    {
        private IEventMessageLogger MessageLogger { get; }

        private IRoadieSettings Configuration { get; }

        public DictionaryCacheManager CacheManager { get; }

        private Encoding.IHttpEncoder HttpEncoder { get; }

        private IRoadieDbContext RoadieDbContext { get; }

        private ILogger Logger
        {
            get
            {
                return MessageLogger as ILogger;
            }
        }

        public LastFmHelperTests()
        {
            MessageLogger = new EventMessageLogger<SearchEngineTests>();
            MessageLogger.Messages += MessageLogger_Messages;

            var settings = new RoadieSettings();
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.test.json");
            IConfiguration configuration = configurationBuilder.Build();
            configuration.GetSection("RoadieSettings").Bind(settings);
            Configuration = settings;
            CacheManager = new DictionaryCacheManager(Logger, new SystemTextCacheSerializer(Logger), new CachePolicy(TimeSpan.FromHours(4)));
            HttpEncoder = new Encoding.DummyHttpEncoder();
        }

        [Fact]
        public async Task LastFMReleaseSearch()
        {
            if (!Configuration.Integrations.LastFmProviderEnabled)
            {
                return;
            }

            var logger = new EventMessageLogger<LastFmHelper>();
            logger.Messages += MessageLogger_Messages;
            var lfmHelper = new LastFmHelper(Configuration, CacheManager, new EventMessageLogger<LastFmHelper>(), RoadieDbContext, HttpEncoder, _httpClientFactory);

            var artistName = "Billy Joel";
            var title = "Piano Man";
            var sw = Stopwatch.StartNew();

            // With Tags
            var result = await lfmHelper.PerformReleaseSearch(artistName, title, 1).ConfigureAwait(false);

            sw.Stop();

            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data);
            var release = result.Data.FirstOrDefault();
            Assert.NotNull(release);

            artistName = "Arkhangelsk";
            title = "Advent";
            sw = Stopwatch.StartNew();

            // Without Tags
            result = await lfmHelper.PerformReleaseSearch(artistName, title, 1).ConfigureAwait(false);

            sw.Stop();

            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data);
            release = result.Data.FirstOrDefault();
            Assert.NotNull(release);
        }

        [Fact]
        public async Task LastFMArtistSearch()
        {
            if (!Configuration.Integrations.LastFmProviderEnabled)
            {
                return;
            }
            var logger = new EventMessageLogger<LastFmHelper>();
            logger.Messages += MessageLogger_Messages;
            var lfmHelper = new LastFmHelper(Configuration, CacheManager, new EventMessageLogger<LastFmHelper>(), RoadieDbContext, HttpEncoder, _httpClientFactory);

            var artistName = "Billy Joel";
            var sw = Stopwatch.StartNew();

            var result = await lfmHelper.PerformArtistSearchAsync(artistName, 1).ConfigureAwait(false);

            sw.Stop();

            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data);
            var release = result.Data.FirstOrDefault();
            Assert.NotNull(release);
        }

        private void MessageLogger_Messages(object sender, EventMessage e)
        {
            Console.WriteLine($"Log Level [{ e.Level }] Log Message [{ e.Message }] ");
        }

    }
}
