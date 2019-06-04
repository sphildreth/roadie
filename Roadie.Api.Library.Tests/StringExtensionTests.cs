using Microsoft.Extensions.Configuration;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Roadie.Library.Tests
{
    public class StringExtensionTests
    {
        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
            return config;
        }

        private readonly IRoadieSettings _settings = null;

        private readonly IConfiguration _configuration;

        private IRoadieSettings Configuration
        {
            get
            {
                return this._settings;
            }
        }

        public StringExtensionTests()
        {
            this._configuration = InitConfiguration();
            this._settings = new RoadieSettings();
            this._configuration.GetSection("RoadieSettings").Bind(this._settings);
        }

        [Theory]
        [InlineData("2:00")]
        [InlineData("30")]
        [InlineData("45:15")]
        [InlineData("5")]
        [InlineData("1:02")]
        public void ToTrackDurationShouldBeGreaterThanZero(string input)
        {
            var t = input.ToTrackDuration();
            Assert.NotNull(t);
            Assert.True(t > 0);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("batman")]
        [InlineData("::")]
        [InlineData("asdfasdfasdfasdfasdfasdfasdf")]
        [InlineData("Q")]
        public void ToTrackDurationShouldBeNull(string input)
        {
            var t = input.ToTrackDuration();
            Assert.Null(t);
        }

        [Fact]
        public void RemoveFirst()
        {
            var test = "02 02-Anything";
            Assert.Equal("02-Anything", test.RemoveFirst("02"));
            test = "02 Anything";
            Assert.Equal("Anything", test.RemoveFirst("02"));
            test = "02 Anything 02";
            Assert.Equal("Anything 02", test.RemoveFirst("02"));
        }

        [Fact]
        public void RemoveStartsWith()
        {
            var test = "02 02 02-Anything";
            Assert.Equal("-Anything", test.RemoveStartsWith("02"));
            test = "02 Anything";
            Assert.Equal("Anything", test.RemoveStartsWith("02"));
            test = "Anything 02";
            Assert.Equal("Anything 02", test.RemoveStartsWith("02"));
            test = "02Anything";
            Assert.Equal("Anything", test.RemoveStartsWith("02"));
            test = "02 02 02 02 Anything 02";
            Assert.Equal("Anything 02", test.RemoveStartsWith("02"));

        }

        [Fact]
        public void StartsWithNumber()
        {
            var test = "This is a test";
            Assert.False(test.DoesStartWithNumber());
            test = "1 This is second test";
            Assert.True(test.DoesStartWithNumber());
            test = "01 This is second test";
            Assert.True(test.DoesStartWithNumber());
            test = "001 This is second test";
            Assert.True(test.DoesStartWithNumber());
            test = "1001 This is second test";
            Assert.True(test.DoesStartWithNumber());
        }

        [Fact]
        public void StripStartingWithNumber()
        {
            var test = "This is a test";
            Assert.Equal("This is a test", test.StripStartingNumber());
            test = "1 This is a test";
            Assert.Equal("This is a test", test.StripStartingNumber());
            test = "01 This is a test";
            Assert.Equal("This is a test", test.StripStartingNumber());
            test = "001 This is a test";
            Assert.Equal("This is a test", test.StripStartingNumber());
            test = "1001 This is a test";
            Assert.Equal("This is a test", test.StripStartingNumber());
        }

        [Fact]
        public void CleanString()
        {
            var t = "[1959] Kind Of Blue";
            Assert.Equal("[1959] Kind Of Blue", t.CleanString(this.Configuration));

            t = "This is a folder name";
            Assert.Equal("This Is A Folder Name", t.CleanString(this.Configuration));

            t = "This is -OBSERVER a folder name";
            Assert.Equal("This Is A Folder Name", t.CleanString(this.Configuration));

            t = "This is [Torrent Tatty] a folder name";
            Assert.Equal("This Is A Folder Name", t.CleanString(this.Configuration));

            t = "This -OBSERVER is [Torrent Tatty] a folder name";
            Assert.Equal("This Is A Folder Name", t.CleanString(this.Configuration));

            t = "[1970] 022 # Plastic Ono Band";
            Assert.Equal("[1970] 022 Plastic Ono Band", t.CleanString(this.Configuration));

        }

        [Fact]
        public void CleanString_Track()
        {

            var t = "11 Love.Mp3".CleanString(this.Configuration, this.Configuration.Processing.TrackRemoveStringsRegex);
            Assert.Equal("Love.Mp3", t);

            t = "99 -Love.Mp3".CleanString(this.Configuration, this.Configuration.Processing.TrackRemoveStringsRegex);
            Assert.Equal("Love.Mp3", t);

            t = "99_Love.Mp3".CleanString(this.Configuration, this.Configuration.Processing.TrackRemoveStringsRegex);
            Assert.Equal("Love.Mp3", t);

            t = "99 _ Love.Mp3".CleanString(this.Configuration, this.Configuration.Processing.TrackRemoveStringsRegex);
            Assert.Equal("Love.Mp3", t);

            t = "001 Love.Mp3".CleanString(this.Configuration, this.Configuration.Processing.TrackRemoveStringsRegex);
            Assert.Equal("Love.Mp3", t);

            t = "01 - Love.Mp3".CleanString(this.Configuration, this.Configuration.Processing.TrackRemoveStringsRegex);
            Assert.Equal("Love.Mp3", t);

            t = "01. Love.Mp3".CleanString(this.Configuration, this.Configuration.Processing.TrackRemoveStringsRegex);
            Assert.Equal("Love.Mp3", t);

            t = "Love.Mp3".CleanString(this.Configuration, this.Configuration.Processing.TrackRemoveStringsRegex);
            Assert.Equal("Love.Mp3", t);

        }

        [Theory]
        [InlineData("Angie (Limited)")]
        [InlineData("Angie CD1")]
        [InlineData("Angie CD 1")]
        [InlineData("Angie CD #2")]
        [InlineData("Angie CD2")]
        [InlineData("Angie CD-2")]
        [InlineData("Angie CD_2")]
        [InlineData("Angie CD23")]
        [InlineData("Angie - CD1")]
        [InlineData("Angie (Limited Edition)")]
        [InlineData("Angie (Deluxe)")]
        [InlineData("Angie (Remastered Deluxe Edition)")]
        [InlineData("Angie (Remastered Deluxe)")]
        [InlineData("Angie ( Deluxe )")]
        [InlineData("Angie (Deluxe Edition)")]
        [InlineData("Angie (Deluxe Expanded Edition)")]
        [InlineData("Angie (2CD Deluxe Edition)")]
        [InlineData("Angie (3CD Deluxe Edition)")]
        [InlineData("Angie [2008 Remastered Edition]")]
        [InlineData("Angie (Bonus CD)")]
        [InlineData("Angie (DELUXE)")]
        [InlineData("Angie (2013, Deluxe Expanded Edition, Disc 1)")]
        [InlineData("Angie (Deluxe Edition, CD1)")]
        [InlineData("Angie (20Th Anniversary Deluxe Edition Remastered)")]
        [InlineData("Angie (Japanese Edition)")]
        [InlineData("Angie (Asian Edition)")]
        [InlineData("Angie (2008 Remastered Edition Digipack)")]
        [InlineData("Angie (Re Release 2003)")]
        [InlineData("Angie [2006, Self Released]")]
        [InlineData("Angie (2002 Expanded Edition)")]
        [InlineData("Angie (2004 Remastered)")]
        [InlineData("Angie (Japan Ltd Dig")] 
        [InlineData("Angie (Japan Release)")]
        public void CleanString_Release_Should_Be_Angie(string input)
        {
            var r = @"(\s*(-\s)*((CD[_\-#\s]*[0-9]*)))|((\(|\[)+([0-9]|,|self|bonus|re(leas|master|(e|d)*)*|th|anniversary|cd|disc|deluxe|dig(ipack)*|vinyl|japan(ese)*|asian|remastered|limited|ltd|expanded|edition|\s)+(]|\)*))";
            var cleaned = input.CleanString(this.Configuration, r);
            Assert.Equal("Angie", cleaned);
        }


        [Theory]
        [InlineData("01 Batman Loves Robin")]
        [InlineData("01  Batman Loves Robin")]
        [InlineData("01 -Batman Loves Robin")]
        [InlineData("01 - Batman Loves Robin")]
        [InlineData("14 Batman Loves Robin")]
        [InlineData("49 Batman Loves Robin")]
        [InlineData("54  Batman Loves Robin")]
        [InlineData("348 Batman Loves Robin")]
        public void Test_Regex_String(string input)
        {
            var t1 = Regex.Replace(input, "^([0-9]+)(\\.|-|\\s)*", "");
            Assert.NotNull(t1);
            Assert.Equal("Batman Loves Robin", t1);

        }

        [Fact]
        public void ToFoldernameFriendly()
        {
            var t = "[1959] Kind Of Blue";
            Assert.Equal("[1959] Kind Of Blue", t.ToFolderNameFriendly());
            t = "This is a folder name";
            Assert.Equal("This is a folder name", t.ToFolderNameFriendly());
            t = "This @Is \\Something".ToFolderNameFriendly();
            Assert.Equal("This @Is Something", t);
            t = "AC\\DC".ToFolderNameFriendly();
            Assert.Equal("AC DC", t);
            t = "3OH*".ToFolderNameFriendly();
            Assert.Equal("3OH", t);
            t = "!BR549".ToFolderNameFriendly();
            Assert.Equal("!BR549", t);
            t = "6?42!88*44".ToFolderNameFriendly();
            Assert.Equal("6 42!88 44", t);
            t = "6?42!88*44?".ToFolderNameFriendly();
            Assert.Equal("6 42!88 44", t);
            t = "L'Être las - L'envers du miroir".ToFolderNameFriendly();
            Assert.Equal("L Être las - L envers du miroir", t);


        }


        [Fact]
        public void ToFileNameFriendly()
        {
            Assert.Equal("08 Specials, The - Pressure Drop", "08 Specials, The - Pressure Drop".ToFileNameFriendly());
            var t = "This @Is \\Something".ToFileNameFriendly();
            Assert.Equal("This @Is Something", t);
            t = "01 The Red fox jumped over the lazy log.".ToFileNameFriendly();
            Assert.Equal("01 The Red fox jumped over the lazy log.", t);
            t = "10 Butchers Tale Western Front 1914.mp3".ToFileNameFriendly();
            Assert.Equal("10 Butchers Tale Western Front 1914.mp3", t);
            t = "[2004] Something Or Another".ToFileNameFriendly();
            Assert.Equal("[2004] Something Or Another", t);
            t = "You Were Great, How Was I? (Duet With Carl Wilson)";
            Assert.Equal("You Were Great, How Was I (Duet With Carl Wilson)", t.ToFileNameFriendly());
            t = "Sitting' On \"Top Of The World!\"";
            Assert.Equal("Sitting On Top Of The World!", t.ToFileNameFriendly());
        }

        [Fact]
        public void ToTitleCase()
        {
            var batmanThe = "Batman, The";

            var n = "Batman";
            Assert.Equal(n, n.ToTitleCase());

            n = "The Batman";
            Assert.Equal(n, n.ToTitleCase(false));

            n = "The    Batman";
            Assert.Equal("The Batman", n.ToTitleCase(false));

            n = "The Batman";
            Assert.Equal(batmanThe, n.ToTitleCase());

            n = "THE BATMAN";
            Assert.Equal(batmanThe, n.ToTitleCase());

            n = "THE    batman";
            Assert.Equal(batmanThe, n.ToTitleCase());

            n = "ThE BatmaN ";
            Assert.Equal(batmanThe, n.ToTitleCase());

            n = " THE BatmaN ";
            Assert.Equal(batmanThe, n.ToTitleCase());

            n = "The Porcupine Tree";
            Assert.Equal("Porcupine Tree, The", n.ToTitleCase());

            n = "He Ain'T Heavy, He'S My Brother.mp3";
            Assert.Equal("He Ain't Heavy, He's My Brother.Mp3", n.ToTitleCase());
        }

        [Fact]
        public void AddToDelimitedList()
        {
            string l = null;
            var r = l.AddToDelimitedList(new string[] { "One" });
            Assert.Equal("One", r);

            l = "One|";
            r = l.AddToDelimitedList(new string[] { "Two" });
            Assert.Equal("One|Two", r);

            l = "One|Two";
            r = l.AddToDelimitedList(new string[3] { "Three", "Four", "Five" });
            Assert.Equal("One|Two|Three|Four|Five", r);

        }


        [Fact]
        public void ParseLargeTrackDuration()
        {
            var ti = new TimeInfo(23372063402);
            Assert.NotNull(ti);
            Assert.True(ti.Seconds > 0);

            var s = ti.ToString();
            var s1 = ti.ToFullFormattedString();
            Assert.Equal(s, s1);

            ti = new TimeInfo(60000);
            Assert.Equal(1, ti.Minutes);

            ti = new TimeInfo(3600000);
            Assert.Equal(1, ti.Hours);
        }

        [Theory]
        [InlineData("19134")]
        [InlineData("23419134")]
        [InlineData("7653419134")]
        [InlineData("771018819134")]
        [InlineData("3271018819134")]
        [InlineData("623271018819134")]
        public void FileSizeFormatProvider(string input)
        {
            var l = (long?)long.Parse(input);
            Assert.True(l > 0);
            var f = l.ToFileSize();
            Assert.NotNull(f);
        }


        [Fact]
        public void SerializeDictionary()
        {
            var d = new Dictionary<string, List<string>>();
            d.Add("One", new List<string> { "OneOneValue", "OneTwoValue", "OneThreeValue" });
            d.Add("Two", new List<string> { "TwoOneValue", "TwoTwoValue", "TwoThreeValue" });

            var s = Newtonsoft.Json.JsonConvert.SerializeObject(d);
            Assert.NotNull(s);

        }

        //[Fact]
        //public void SerializeArtistXmlResult()
        //{
        //    var artist = new roadie.Models.Subsonic.ArtistXmlResponse
        //    {
        //        artist = new roadie.Models.Subsonic.artist
        //        {
        //            name = "Colin Hay"
        //        }
        //    };
        //    XmlSerializer xsSubmit = new XmlSerializer(typeof(roadie.Models.Subsonic.ArtistXmlResponse));

        //    var xml = "";
        //    using (var sww = new StringWriter())
        //    {
        //        using (XmlWriter writer = XmlWriter.Create(sww))
        //        {
        //            xsSubmit.Serialize(writer, artist);
        //            xml = sww.ToString(); // Your XML
        //        }
        //    }
        //    Assert.NotNull(xml);
        //}

    }
}
