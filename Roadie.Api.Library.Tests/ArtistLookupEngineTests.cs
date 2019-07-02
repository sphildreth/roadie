using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Engines;
using Roadie.Library.Extensions;
using Roadie.Library.Factories;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.Processors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Roadie.Library.Tests
{
    public class ArtistLookupEngineTests
    {
        private IEventMessageLogger MessageLogger { get; }
        private ILogger Logger
        {
            get
            {
                return this.MessageLogger as ILogger;
            }
        }

        

        private IRoadieSettings Configuration { get; }
        public DictionaryCacheManager CacheManager { get; }
        private Encoding.IHttpEncoder HttpEncoder { get; }

        public ArtistLookupEngineTests()
        {
            this.MessageLogger = new EventMessageLogger();
            this.MessageLogger.Messages += MessageLogger_Messages;

            var settings = new RoadieSettings();
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.test.json");
            IConfiguration configuration = configurationBuilder.Build();
            configuration.GetSection("RoadieSettings").Bind(settings);
            this.Configuration = settings;
            this.CacheManager = new DictionaryCacheManager(this.Logger, new CachePolicy(TimeSpan.FromHours(4)));
            this.HttpEncoder = new Encoding.DummyHttpEncoder();
        }

        private void MessageLogger_Messages(object sender, EventMessage e)
        {
            Console.WriteLine($"Log Level [{ e.Level }] Log Message [{ e.Message }] ");
        }

        [Fact]
        public void Get_Artist_By_Name()
        {
            var optionsBuilder = new DbContextOptionsBuilder<RoadieDbContext>();
            optionsBuilder.UseMySql("server=voyager;userid=roadie;password=MenAtW0rk668;persistsecurityinfo=True;database=roadie;ConvertZeroDateTime=true");

            using (var context = new RoadieDbContext(optionsBuilder.Options))
            {
                IArtistLookupEngine artistLookupEngine = new ArtistLookupEngine(this.Configuration, this.HttpEncoder, context, this.CacheManager, this.Logger);
                var a = artistLookupEngine.DatabaseQueryForArtistName("Nas");
            }

        }

        //[Fact]
        //public void Update_Releases_Special_Name()
        //{
        //    var optionsBuilder = new DbContextOptionsBuilder<RoadieDbContext>();
        //    optionsBuilder.UseMySql("server=viking;userid=roadie;password=MenAtW0rk668;persistsecurityinfo=True;database=roadie;ConvertZeroDateTime=true");

        //    using (var context = new RoadieDbContext(optionsBuilder.Options))
        //    {
        //        var now = DateTime.UtcNow;
        //        foreach (var release in context.Releases)
        //        {
        //            var releaseModel = release.Adapt<Roadie.Library.Models.Releases.Release>();
        //            var specialReleaseTitle = release.Title.ToAlphanumericName();
        //            if (!releaseModel.AlternateNamesList.Contains(specialReleaseTitle, StringComparer.OrdinalIgnoreCase))
        //            {
        //                var alt = new List<string>(releaseModel.AlternateNamesList)
        //                {
        //                    specialReleaseTitle
        //                };
        //                release.AlternateNames = alt.ToDelimitedList();
        //                release.LastUpdated = now;
        //            }
        //        }
        //        context.SaveChanges();
        //    }
        //}

        //[Fact]
        //public void Update_Artist_Special_Name()
        //{
        //    var optionsBuilder = new DbContextOptionsBuilder<RoadieDbContext>();
        //    optionsBuilder.UseMySql("server=viking;userid=roadie;password=MenAtW0rk668;persistsecurityinfo=True;database=roadie;ConvertZeroDateTime=true");

        //    using (var context = new RoadieDbContext(optionsBuilder.Options))
        //    {
        //        var now = DateTime.UtcNow;
        //        foreach (var artist in context.Artists)
        //        {
        //            var artistModel = artist.Adapt<Roadie.Library.Models.Artist>();
        //            var specialArtistName = artist.Name.ToAlphanumericName();
        //            if (!artistModel.AlternateNamesList.Contains(specialArtistName, StringComparer.OrdinalIgnoreCase))
        //            {
        //                var alt = new List<string>(artistModel.AlternateNamesList)
        //                {
        //                    specialArtistName
        //                };
        //                artist.AlternateNames = alt.ToDelimitedList();
        //                artist.LastUpdated = now;
        //            }
        //        }
        //        context.SaveChanges();
        //    }
        //}
    }
}
