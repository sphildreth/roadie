using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Processors;
using Roadie.Library.SearchEngines.MetaData.Discogs;
using System;
using System.Collections.Generic;
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

        private void MessageLogger_Messages(object sender, EventMessage e)
        {
            Console.WriteLine($"Log Level [{ e.Level }] Log Message [{ e.Message }] ");
        }

    }
}
