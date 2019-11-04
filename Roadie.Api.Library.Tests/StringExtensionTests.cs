using Microsoft.Extensions.Configuration;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.Utility;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xunit;

namespace Roadie.Library.Tests
{
    public class StringExtensionTests
    {
        private readonly IConfiguration _configuration;

        private readonly IRoadieSettings _settings = null;

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

        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
            return config;
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

        [Theory]
        [InlineData("[1959] Kind Of Blue", "[1959] Kind Of Blue")]
        [InlineData("This Is A Folder Name", "This Is A Folder Name")]
        [InlineData("[1970] 022 # Plastic Ono Band", "[1970] 022 Plastic Ono Band")]
        [InlineData("This is -OBSERVER a folder name", "This Is A Folder Name")]
        [InlineData("This is [Torrent Tatty] a folder name", "This Is A Folder Name")]
        [InlineData("This -OBSERVER is [Torrent Tatty] a folder name", "This Is A Folder Name")]
        public void CleanString(string input, string shouldBe)
        {
            Assert.Equal(shouldBe, input.CleanString(Configuration));
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
        public void CleanStringReleaseShouldBeAngie(string input)
        {
            var r = @"(\s*(-\s)*((CD[_\-#\s]*[0-9]*)))|((\(|\[)+([0-9]|,|self|bonus|re(leas|master|(e|d)*)*|th|anniversary|cd|disc|deluxe|dig(ipack)*|vinyl|japan(ese)*|asian|remastered|limited|ltd|expanded|edition|\s)+(]|\)*))";
            var cleaned = input.CleanString(this.Configuration, r);
            Assert.Equal("Angie", cleaned);
        }

        [Theory]
        [InlineData("11 Love.Mp3")]
        [InlineData("99 -Love.Mp3")]
        [InlineData("99_Love.Mp3")]
        [InlineData("01. Love.Mp3")]
        [InlineData("99 _ Love.Mp3")]
        [InlineData("001 Love.Mp3")]
        [InlineData("01 - Love.Mp3")]
        [InlineData("Love.Mp3")]
        [InlineData("LOVE.Mp3")]
        [InlineData("love.mp3")]
        public void CleanStringTrack(string input)
        {
            Assert.Equal("Love.Mp3", input.CleanString(Configuration, Configuration.Processing.TrackRemoveStringsRegex).ToTitleCase());
        }

        [Theory]
        [InlineData("This is a test")]
        [InlineData(" This is a test")]
        [InlineData("One Two Three")]
        public void DoesNotStartsWithNumber(string input)
        {
            Assert.False(input.DoesStartWithNumber());
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
        [InlineData("02 02-Anything", "02-Anything")]
        [InlineData("02 Anything", "Anything")]
        [InlineData("02 Anything 02", "Anything 02")]
        public void RemoveFirst(string input, string shouldBe)
        {
            Assert.Equal(shouldBe, input.RemoveFirst("02"));
        }

        [Theory]
        [InlineData("02 02 02-Anything", "-Anything")]
        [InlineData("02 Anything", "Anything")]
        [InlineData("Anything 02", "Anything 02")]
        [InlineData("02Anything", "Anything")]
        [InlineData("02 02 02 02 Anything 02", "Anything 02")]
        public void RemoveStartsWith(string input, string shouldBe)
        {
            Assert.Equal(shouldBe, input.RemoveStartsWith("02"));
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

        [Theory]
        [InlineData("1 This is second test")]
        [InlineData("01 This is second test")]
        [InlineData("98 This is second test")]
        [InlineData("001 This is second test")]
        [InlineData("1001 This is second test")]
        public void StartsWithNumber(string input)
        {
            Assert.True(input.DoesStartWithNumber());
        }

        [Theory]
        [InlineData("This is a test", "This is a test")]
        [InlineData("1 This is a test", "This is a test")]
        [InlineData("01 This is a test", "This is a test")]
        [InlineData("76 This is a test", "This is a test")]
        [InlineData("1001 This is a test", "This is a test")]
        public void StripStartingWithNumber(string input, string shouldBe)
        {
            Assert.Equal(shouldBe, input.StripStartingNumber());
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
        public void TestRegexString(string input)
        {
            var t1 = Regex.Replace(input, "^([0-9]+)(\\.|-|\\s)*", "");
            Assert.NotNull(t1);
            Assert.Equal("Batman Loves Robin", t1);
        }

        [Theory]
        [InlineData("Bob", "bob")]
        [InlineData("Bob Marley", "bobmarley")]
        [InlineData("Ringo Starr And His All-Starr Band", "ringostarrandhisallstarrband")]
        [InlineData("Leslie & Tom", "leslieandtom")]
        [InlineData(" Leslie  &  Tom", "leslieandtom")]
        [InlineData("C o l i n  H a y", "colinhay")]
        [InlineData("ColinHay", "colinhay")]
        [InlineData("Colin Hay!", "colinhay")]
        [InlineData("colinhay", "colinhay")]
        [InlineData("COLINHAY", "colinhay")]
        [InlineData("C.O!L&quot;I$N⌐HƒAY;", "colinhay")]
        [InlineData(" Leslie  &amp; Tom", "leslieandtom")]
        [InlineData("<b>Leslie  &amp; &#32;&#32; Tom</b>", "leslieandtom")]
        [InlineData("Leslie;/&/;Tom", "leslieandtom")]
        [InlineData("Leslie And Tom", "leslieandtom")]
        [InlineData("L≈esl|ie ƒand T╗om╣;", "leslieandtom")]
        [InlineData("Leslie Tom", "leslietom")]
        [InlineData("Hüsker Dü", "huskerdu")]
        [InlineData("Motörhead", "motorhead")]
        [InlineData("Alright, Still", "alrightstill")]
        [InlineData("Something,  SOMETHING & somEthing!", "somethingsomethingandsomething")]
        [InlineData("comfort y mãºsica para volar", "comfortymasicaparavolar")]
        [InlineData("canciã³n animal", "canciananimal")]
        [InlineData("Xylø", "xyloe")]
        [InlineData("Метель", "metel")]
        [InlineData("Svartidauði", "svartidaudhi")]
        public void ToAlphanumericNameShouldStripAndMatch(string input, string shouldBe)
        {
            Assert.Equal(shouldBe, input.ToAlphanumericName());
        }

        [Theory]
        [InlineData("08 Specials, The - Pressure Drop", "08 Specials, The - Pressure Drop")]
        [InlineData("This @Is \\Something", "This @Is Something")]
        [InlineData("01 The Red fox jumped over the lazy log.", "01 The Red fox jumped over the lazy log.")]
        [InlineData("You Were Great, How Was I? (Duet With Carl Wilson)", "You Were Great, How Was I (Duet With Carl Wilson)")]
        [InlineData("10 Butchers Tale Western Front 1914.mp3", "10 Butchers Tale Western Front 1914.mp3")]
        [InlineData("[2004] Something Or Another", "[2004] Something Or Another")]
        [InlineData("Sitting' On \"Top Of The World!\"", "Sitting On Top Of The World!")]
        public void ToFileNameFriendly(string input, string shouldBe)
        {
            Assert.Equal(shouldBe, input.ToFileNameFriendly());
        }

        [Theory]
        [InlineData("[1959] Kind Of Blue", "[1959] Kind Of Blue")]
        [InlineData("This is a folder name", "This is a folder name")]
        [InlineData("This @Is \\Something", "This @Is Something")]
        [InlineData("AC\\DC", "AC DC")]
        [InlineData("3OH*", "3OH")]
        [InlineData("!BR549", "!BR549")]
        [InlineData("6?42!88*44", "6 42!88 44")]
        [InlineData("6?42!88*44?", "6 42!88 44")]
        [InlineData("L'Être las - L'envers du miroir", "L Être las - L envers du miroir")]
        public void ToFoldernameFriendly(string input, string shouldBe)
        {
            Assert.Equal(shouldBe, input.ToFolderNameFriendly());
        }

        [Theory]
        [InlineData("Batman", "Batman")]
        [InlineData("40K Freebie", "40k Freebie")]
        [InlineData("Live From Austin Texas", "Live From Austin Texas")]
        [InlineData("LIVE FROM AUSTIN TEXAS", "Live From Austin Texas")]
        [InlineData("live from austin texas", "Live From Austin Texas")]
        [InlineData("live from AUSTIN TEXAS", "Live From Austin Texas")]
        [InlineData("Count the 50 ways I love you", "Count The 50 Ways I Love You")]
        [InlineData("Count the 50 ways to love you", "Count The 50 Ways To Love You")]
        [InlineData("Opus IV", "Opus IV")]
        [InlineData("OPUS IV", "Opus IV")]
        [InlineData("opus iv", "Opus IV")]
        [InlineData("He Ain'T Heavy, He'S My Brother.mp3", "He Ain't Heavy, He's My Brother.Mp3")]
        [InlineData("The Porcupine Tree", "Porcupine Tree, The")]
        [InlineData("The Batman", "Batman, The")]
        [InlineData("The    Batman", "Batman, The")]
        [InlineData("THE BATMAN", "Batman, The")]
        [InlineData("THE    batman", "Batman, The")]
        [InlineData("ThE BatmaN", "Batman, The")]
        [InlineData(" THE BatmaN ", "Batman, The")] 
        [InlineData("Don’T Threaten Me With A Good Time", "Don't Threaten Me With A Good Time")] 
        [InlineData("Don’t Threaten Me With A Good Time", "Don't Threaten Me With A Good Time")] 
        [InlineData("don’t threaten me with a good time", "Don't Threaten Me With A Good Time")] 
        [InlineData("DON'T THREATEN ME WITH A GOOD TIME", "Don't Threaten Me With A Good Time")]
        [InlineData("McDonald's", "McDonald's")]
        [InlineData("Macey Grey", "Macey Grey")]
        [InlineData("macey grey", "Macey Grey")]
        [InlineData("MACEY GREY", "Macey Grey")]
        [InlineData("Reggie O'Reilly", "Reggie O'Reilly")]
        [InlineData("REGGIE O'REILLY", "Reggie O'Reilly")]
        [InlineData("reggie o'reilly", "Reggie O'Reilly")]
        [InlineData("Billy Joel", "Billy Joel")]
        [InlineData("Billy JOEL", "Billy Joel")]
        [InlineData("billy joel", "Billy Joel")]
        [InlineData("bIlLy jOeL", "Billy Joel")]
        [InlineData("dick van dyke", "Dick Van Dyke")]
        [InlineData("Dick van Dyke", "Dick Van Dyke")]
        [InlineData("DICK VAN DYKE", "Dick Van Dyke")] 
        [InlineData("Michael Mcdonald", "Michael McDonald")]
        [InlineData("MICHAEL MCDONALD", "Michael McDonald")]
        [InlineData("delbert mcclinton", "Delbert McClinton")]
        [InlineData("duff mckagan", "Duff McKagan")]
        [InlineData("george mccrae", "George McCrae")]
        [InlineData("mary macgregor", "Mary MacGregor")]
        [InlineData("mary macfarlane", "Mary MacFarlane")]
        [InlineData("mary Mccutcheon", "Mary McCutcheon")]
        [InlineData("mary Mcferrin", "Mary McFerrin")]
        [InlineData("mary mccall", "Mary McCall")]
        [InlineData("mary mccombs", "Mary McCombs")]
        [InlineData("don mcclean", "Don McClean")]
        [InlineData("edWIN mccain", "Edwin McCain")]
        [InlineData("kyle mcevoy", "Kyle McEvoy")]
        [InlineData("c.w. mccall", "C.W. McCall")] 
        [InlineData("Loreena Mckennitt", "Loreena McKennitt")] 
        public void ToTitleCase(string input, string shouldBe)
        {
            Assert.Equal(shouldBe, input.ToTitleCase());
        }

        [Theory]
        [InlineData("Batman", "Batman")]
        [InlineData("He Ain'T Heavy, He'S My Brother.mp3", "He Ain't Heavy, He's My Brother.Mp3")]
        [InlineData("The Porcupine Tree", "The Porcupine Tree")]
        [InlineData("The Batman", "The Batman")]
        public void ToTitleCaseDoPutTheAtEnd(string input, string shouldBe)
        {
            Assert.Equal(shouldBe, input.ToTitleCase(false));
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


        [Theory]
        [InlineData("https://itunes.apple.com/us/artist/id485953", "id485953")]
        [InlineData("https://www.last.fm/music/Billy+Joel", "Billy+Joel")]
        [InlineData("https://www.discogs.com/artist/137418", "137418")]
        public void LastSegmentInUrl(string input, string shouldBe)
        {
            var v = input.LastSegmentInUrl();
            Assert.Equal(v, shouldBe);
        }
    }
}