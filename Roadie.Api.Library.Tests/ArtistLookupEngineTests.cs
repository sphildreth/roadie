﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Processors;
using System;

namespace Roadie.Library.Tests
{
    public class ArtistLookupEngineTests
    {
        private IEventMessageLogger MessageLogger { get; }

        private ILogger Logger
        {
            get
            {
                return MessageLogger as ILogger;
            }
        }

        private IRoadieSettings Configuration { get; }

        public DictionaryCacheManager CacheManager { get; }

        private Encoding.IHttpEncoder HttpEncoder { get; }

        public ArtistLookupEngineTests()
        {
            MessageLogger = new EventMessageLogger<ArtistLookupEngineTests>();
            MessageLogger.Messages += MessageLogger_Messages;

            var settings = new RoadieSettings();
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.test.json");
            IConfiguration configuration = configurationBuilder.Build();
            configuration.GetSection("RoadieSettings").Bind(settings);
            Configuration = settings;
            CacheManager = new DictionaryCacheManager(Logger, new NewtonsoftCacheSerializer(Logger), new CachePolicy(TimeSpan.FromHours(4)));
            HttpEncoder = new Encoding.DummyHttpEncoder();
        }

        private void MessageLogger_Messages(object sender, EventMessage e)
        {
            Console.WriteLine($"Log Level [{ e.Level }] Log Message [{ e.Message }] ");
        }
    }
}
