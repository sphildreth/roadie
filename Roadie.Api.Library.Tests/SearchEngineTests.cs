using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData.MusicBrainz;
using Roadie.Library.Processors;
using Roadie.Library.SearchEngines.MetaData.Discogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Roadie.Library.Tests
{
    public class SearchEngineTests
    {
        private IEventMessageLogger MessageLogger { get; }
        private ILogger Logger
        {
            get
            {
                return MessageLogger as ILogger;
            }
        }

        public SearchEngineTests()
        {
            MessageLogger = new EventMessageLogger<SearchEngineTests>();
            MessageLogger.Messages += MessageLogger_Messages;

            var settings = new RoadieSettings();
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.test.json");
            IConfiguration configuration = configurationBuilder.Build();
            configuration.GetSection("RoadieSettings").Bind(settings);
            Configuration = settings;
            CacheManager = new DictionaryCacheManager(Logger, new CachePolicy(TimeSpan.FromHours(4)));
            HttpEncoder = new Encoding.DummyHttpEncoder();
        }



        private IRoadieSettings Configuration { get; }
        public DictionaryCacheManager CacheManager { get; }
        private Encoding.IHttpEncoder HttpEncoder { get; }

        [Fact]
        public async Task DiscogsHelperReleaseSearch()
        {
            if(!Configuration.Integrations.DiscogsProviderEnabled)
            {
                return;
            }
            var discogsLogger = new EventMessageLogger<DiscogsHelper>();
            discogsLogger.Messages += MessageLogger_Messages;

            var engine = new DiscogsHelper(Configuration, CacheManager, discogsLogger);

            var artistName = "With The Dead";
            var title = "Love From With The Dead";

            var result = await engine.PerformReleaseSearch(artistName, title, 1);
            Assert.NotNull(result);
            Assert.NotEmpty(result.Data);

        }

        [Fact]
        public async Task MusicBrainzArtistSearch()
        {
            if (!Configuration.Integrations.MusicBrainzProviderEnabled)
            {
                return;
            }
            var logger = new EventMessageLogger<MusicBrainzProvider>();
            logger.Messages += MessageLogger_Messages;
            var mb = new MusicBrainzProvider(Configuration, CacheManager, logger);

            var artistName = "Billy Joel";
            var mbId = "64b94289-9474-4d43-8c93-918ccc1920d1"; //https://musicbrainz.org/artist/64b94289-9474-4d43-8c93-918ccc1920d1

            var sw = Stopwatch.StartNew();

            var result = await mb.PerformArtistSearch(artistName, 1);

            sw.Stop();
            var elapsedTime = sw.ElapsedMilliseconds;

            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data);
            var artist = result.Data.FirstOrDefault();
            Assert.NotNull(artist);
            Assert.Equal(artist.MusicBrainzId, mbId);
        }

        [Fact]
        public async Task MusicBrainzReleaseSearch()
        {
            if (!Configuration.Integrations.MusicBrainzProviderEnabled)
            {
                return;
            }
            var logger = new EventMessageLogger<MusicBrainzProvider>();
            logger.Messages += MessageLogger_Messages;
            var mb = new MusicBrainzProvider(Configuration, CacheManager, logger);

            var artistName = "Billy Joel";
            var mbId = "584a3887-c74e-4ff2-81e1-1620fbbe4f84"; // https://musicbrainz.org/release/584a3887-c74e-4ff2-81e1-1620fbbe4f84
            var title = "Piano Man";
            var sw = Stopwatch.StartNew();

            var result = await mb.PerformReleaseSearch(artistName, title, 1);

            sw.Stop();
            var elapsedTime = sw.ElapsedMilliseconds;

            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data);
            var release = result.Data.FirstOrDefault();
            Assert.NotNull(release);
            Assert.Equal(release.MusicBrainzId, mbId);
        }


        private void MessageLogger_Messages(object sender, EventMessage e)
        {
            Console.WriteLine($"Log Level [{ e.Level }] Log Message [{ e.Message }] ");
        }

    }
}
