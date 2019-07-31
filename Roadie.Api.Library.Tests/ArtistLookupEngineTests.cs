using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Engines;
using Roadie.Library.Extensions;
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
            this.MessageLogger = new EventMessageLogger<ArtistLookupEngineTests>();
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


        //[Fact]
        //public void Update_Genre_Normalized_Name()
        //{
        //    var optionsBuilder = new DbContextOptionsBuilder<RoadieDbContext>();
        //    optionsBuilder.UseMySql("server=viking;userid=roadie;password=MenAtW0rk668;persistsecurityinfo=True;database=roadie;ConvertZeroDateTime=true");

        //    using (var context = new RoadieDbContext(optionsBuilder.Options))
        //    {
        //        var now = DateTime.UtcNow;
        //        foreach (var genre in context.Genres)
        //        {
        //            genre.NormalizedName = genre.Name.ToAlphanumericName();
        //            genre.LastUpdated = now;
        //        }
        //        context.SaveChanges();
        //    }
        //}

        //[Fact]
        //public void Merge_Genres()
        //{
        //    var optionsBuilder = new DbContextOptionsBuilder<RoadieDbContext>();
        //    optionsBuilder.UseMySql("server=viking;userid=roadie;password=MenAtW0rk668;persistsecurityinfo=True;database=roadie;ConvertZeroDateTime=true");

        //    using (var context = new RoadieDbContext(optionsBuilder.Options))
        //    {
        //        var addedArtistToGenre = new List<KeyValuePair<int, int>>();
        //        var addedReleaseToGenre = new List<KeyValuePair<int, int>>();
        //        var now = DateTime.UtcNow;
        //        var groupedGenres = context.Genres.GroupBy(x => x.NormalizedName).ToArray();
        //        foreach (var genreGroup in groupedGenres)
        //        {
        //            var genre = genreGroup.OrderBy(x => x.Id).First();
        //            foreach (var gg in genreGroup.OrderBy(x => x.Id).Skip(1))
        //            {
        //                var artistIdsInDups = (from g in context.Genres
        //                                        join ag in context.ArtistGenres on g.Id equals ag.GenreId
        //                                        where g.Id == gg.Id
        //                                        select ag.ArtistId).Distinct().ToArray();

        //                var releaseIdsInDups = (from g in context.Genres
        //                                        join rg in context.ReleaseGenres on g.Id equals rg.GenreId
        //                                        where g.Id == gg.Id
        //                                        select rg.ReleaseId).Distinct().ToArray();

        //                if (artistIdsInDups != null && artistIdsInDups.Any())
        //                {
        //                    foreach (var artistIdsInDup in artistIdsInDups)
        //                    {
        //                        if (!addedArtistToGenre.Any(x => x.Key == artistIdsInDup && x.Value == genre.Id))
        //                        {
        //                            context.ArtistGenres.Add(new ArtistGenre
        //                            {
        //                                ArtistId = artistIdsInDup,
        //                                GenreId = genre.Id
        //                            });
        //                            addedArtistToGenre.Add(new KeyValuePair<int, int>(artistIdsInDup, genre.Id));
        //                        }
        //                    }
        //                }

        //                if (releaseIdsInDups != null && releaseIdsInDups.Any())
        //                {
        //                    foreach (var releaseIdsInDup in releaseIdsInDups)
        //                    {
        //                        if (!addedReleaseToGenre.Any(x => x.Key == releaseIdsInDup && x.Value == genre.Id))
        //                        {
        //                            context.ReleaseGenres.Add(new ReleaseGenre
        //                            {
        //                                ReleaseId = releaseIdsInDup,
        //                                GenreId = genre.Id
        //                            });
        //                            addedReleaseToGenre.Add(new KeyValuePair<int, int>(releaseIdsInDup, genre.Id));
        //                        }
        //                    }
        //                }

        //                context.Genres.Remove(gg);
        //                context.SaveChanges();
        //            }
        //        }
        //    }
        //}


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

        //[Fact]
        //public void Update_Label_Special_Name()
        //{
        //    var optionsBuilder = new DbContextOptionsBuilder<RoadieDbContext>();
        //    optionsBuilder.UseMySql("server=viking;userid=roadie;password=MenAtW0rk668;persistsecurityinfo=True;database=roadie;ConvertZeroDateTime=true");

        //    using (var context = new RoadieDbContext(optionsBuilder.Options))
        //    {
        //        var now = DateTime.UtcNow;
        //        foreach (var label in context.Labels)
        //        {
        //            var labelModel = label.Adapt<Roadie.Library.Models.Label>();
        //            var specialLabelName = labelModel.Name.ToAlphanumericName();
        //            if (!labelModel.AlternateNamesList.Contains(specialLabelName, StringComparer.OrdinalIgnoreCase))
        //            {
        //                var alt = new List<string>(labelModel.AlternateNamesList)
        //                {
        //                    specialLabelName
        //                };
        //                label.AlternateNames = alt.ToDelimitedList();
        //                label.LastUpdated = now;
        //            }
        //        }
        //        context.SaveChanges();
        //    }
        //}

    }
}
