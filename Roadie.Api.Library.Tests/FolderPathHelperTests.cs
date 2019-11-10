using Microsoft.Extensions.Configuration;
using Roadie.Library.Configuration;
using Roadie.Library.Utility;
using System.IO;
using Xunit;

namespace Roadie.Library.Tests
{
    public class FolderPathHelperTests
    {
        private IRoadieSettings Configuration { get; }

        public FolderPathHelperTests()
        {
            var settings = new RoadieSettings();
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.test.json");
            IConfiguration configuration = configurationBuilder.Build();
            configuration.GetSection("RoadieSettings").Bind(settings);
            this.Configuration = settings;
        }

        [Theory]
        [InlineData("Bob Jones", 1, @"B\BO\Bob Jones [1]")]
        [InlineData("Smith, Angie", 2, @"S\SM\Smith, Angie [2]")]
        [InlineData("Blues Traveler", 89, @"B\BL\Blues Traveler [89]")]
        [InlineData("Scott, Travis", 3, @"S\SC\Scott, Travis [3]")]
        [InlineData("Straight, George", 783, @"S\ST\Straight, George [783]")]
        [InlineData("Good! Times?", 213, @"G\GO\Good Times [213]")]
        [InlineData("3rd Attempt, The", 56, @"0\3R\3rd Attempt, The [56]")]
        [InlineData("3Oh!3", 589, @"0\3O\3oh3 [589]")]
        [InlineData(" 3OH!3", 589, @"0\3O\3oh3 [589]")]
        [InlineData("3oh!3 ", 589, @"0\3O\3oh3 [589]")]
        [InlineData("B", 3423, @"B\B\B [3423]")]
        [InlineData("BB", 3423, @"B\BB\Bb [3423]")]
        [InlineData("Bad.Boys.Club", 433423, @"B\BA\Bad Boys Club [433423]")]
        [InlineData("Bad_Boys_Club", 433423, @"B\BA\Bad Boys Club [433423]")]
        [InlineData("Bad.Boys Club", 433423, @"B\BA\Bad Boys Club [433423]")]
        [InlineData("Bad~Boys~Club", 433423, @"B\BA\Bad Boys Club [433423]")]
        [InlineData("Bad-Boys=Club", 433423, @"B\BA\Bad Boys Club [433423]")]
        [InlineData("Bad.Boys~Club_2", 433423, @"B\BA\Bad Boys Club 2 [433423]")]
        [InlineData("13", 43234, @"0\13\13 [43234]")]
        [InlineData("69 Eyes, The", 123, @"0\69\69 Eyes, The [123]")]
        [InlineData("2 Unlimited", 234, @"0\2U\2 Unlimited [234]")]
        [InlineData("A Flock Of Seagulls", 567, @"A\AF\A Flock Of Seagulls [567]")]
        [InlineData("1975, The", 23, @"0\19\1975, The [23]")]
        [InlineData("1914", 89, @"0\19\1914 [89]")]
        [InlineData("13th Floor Elevators, The", 24, @"0\13\13th Floor Elevators, The [24]")]
        [InlineData("13TH FLOOR ELEVATORS, THE", 24, @"0\13\13th Floor Elevators, The [24]")]
        [InlineData("13th floor elevators, the", 24, @"0\13\13th Floor Elevators, The [24]")]
        [InlineData("3 Doors Down", 11111111, @"0\3D\3 Doors Down [11111111]")]
        [InlineData(" 3    Doors   Down", 11111111, @"0\3D\3 Doors Down [11111111]")]
        [InlineData("01234567890123456789012345678901234567890123456789", 123, @"0\01\01234567890123456789012345678901234567890123456789 [123]")]
        [InlineData("012345678901234567890", 1111111111, @"0\01\012345678901234567890 [1111111111]")]
        [InlineData("Royce Da 5'9\"", 876, @"R\RO\Royce Da 59 [876]")]
        [InlineData("8", 434, @"0\8\8 [434]")]
        [InlineData("Hay, Colin", 888, @"H\HA\Hay, Colin [888]")]
        [InlineData("Ty Dolla $Ign", 6542, @"T\TY\Ty Dolla Sign [6542]")] //
        [InlineData("Panic! At The Disco", 32, @"P\PA\Panic At The Disco [32]")]
        [InlineData("Ke$ha", 98, @"K\KE\Kesha [98]")]
        [InlineData("(Whispers)", 432, @"W\WH\Whispers [432]")]
        [InlineData("Whiskers, John \"Catfish\"", 32342, @"W\WH\Whiskers, John Catfish [32342]")]
        [InlineData("'68", 54321, @"0\68\68 [54321]")]
        [InlineData("2Pac", 987654321, @"0\2P\2pac [987654321]")]
        [InlineData("007 Superhero", 1, @"0\00\007 Superhero [1]")]
        [InlineData("4 Non Blondes", 666, @"0\4N\4 Non Blondes [666]")]
        [InlineData("Dream Theater", 99, @"D\DR\Dream Theater [99]")]
        [InlineData("A.X.E. Project, The", 100, @"A\AX\A X E Project, The [100]")]
        [InlineData("IRON MAIDEN", 9909, @"I\IR\Iron Maiden [9909]")]
        [InlineData("iRoN   MaiDEn", 9909, @"I\IR\Iron Maiden [9909]")]
        [InlineData(" iRoN   MaiDEn  ", 9909, @"I\IR\Iron Maiden [9909]")]
        [InlineData(" i  R   o    N   M  a   i    D E   n  ", 9909, @"I\IR\I R O N M A I D E N [9909]")]
        [InlineData("iron maiden", 9909, @"I\IR\Iron Maiden [9909]")]
        [InlineData("Dreamside, The", 85, @"D\DR\Dreamside, The [85]")]
        [InlineData("Stacy & Tom", 41, @"S\ST\Stacy And Tom [41]")]
        [InlineData("A! Whack To The Head", 2111, @"A\AW\A Whack To The Head [2111]")]
        [InlineData("!WHACKO!", 2112, @"W\WH\Whacko [2112]")]
        [InlineData("Öysterhead", 7653, @"O\OY\Oysterhead [7653]")]
        [InlineData("Öystërheäd", 7653, @"O\OY\Oysterhead [7653]")]
        [InlineData("2Nu", 7653, @"0\2N\2nu [7653]")]
        [InlineData("!SÖmëthing el$e", 73223, @"S\SO\Something Else [73223]")]
        [InlineData("McDonald, Audra", 1232, @"M\MC\McDonald, Audra [1232]")]
        [InlineData("McKnight, Brian", 1233, @"M\MC\McKnight, Brian [1233]")]
        [InlineData("Womack, Lee Ann", 1234, @"W\WO\Womack, Lee Ann [1234]")]
        [InlineData("At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint occaecati cupiditate non providenta", 123456789, @"A\AT\At Vero Eos Et Accusamus Et Iusto Odio Dignissimos Ducimus Qui Blanditiis Praesent [123456789]")]
        public void GenerateArtistFolderNames(string input, int artistId, string shouldBe)
        {
            var artistFolder = FolderPathHelper.ArtistPath(Configuration, artistId, input);
            var t = new DirectoryInfo(Path.Combine(Configuration.LibraryFolder, shouldBe));
            Assert.Equal(t.FullName, artistFolder);
        }

        [Theory]
        [InlineData("!SÖmëthing el$e", @"S\SO")]
        [InlineData("A&M Records", @"A\AA")]
        [InlineData("A!M Records", @"A\AM")]
        [InlineData("Aware Records", @"A\AW")]
        [InlineData("Jive", @"J\JI")]
        [InlineData("Le Plan", @"L\LE")]
        [InlineData("42 Records", @"0\42")]
        public void GenerateLabelFolderNames(string input, string shouldBe)
        {
            var artistFolder = FolderPathHelper.LabelPath(Configuration, input);
            var t = new DirectoryInfo(Path.Combine(Configuration.LabelImageFolder, shouldBe));
            Assert.Equal(t.FullName, artistFolder);
        }

        [Theory]
        [InlineData("!SÖmëthing el$e", @"S\SO")]
        [InlineData("Alternative Singer/Songwriter", @"A\AL")]
        [InlineData("Adult Alternative", @"A\AD")]
        [InlineData("Progressive Bluegrass", @"P\PR")]
        [InlineData("Western European Traditions", @"W\WE")]
        [InlineData("2-Step/British Garage", @"0\2S")]
        [InlineData("80's/synthwave/outrun/new retro wave", @"0\80")]
        [InlineData("Western Swing Revival", @"W\WE")]
        public void GenerateGenreFolderNames(string input, string shouldBe)
        {
            var artistFolder = FolderPathHelper.GenrePath(Configuration, input);
            var t = new DirectoryInfo(Path.Combine(Configuration.GenreImageFolder, shouldBe));
            Assert.Equal(t.FullName, artistFolder);
        }

        [Theory]
        [InlineData("What Dreams May Come", "01/15/2004", @"D\DR\Dream Theater [99]", @"D\DR\Dream Theater [99]\[2004] What Dreams May Come")]
        [InlineData("Killers", "01/01/1980", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1980] Killers")]
        [InlineData("Original Hits (80S 12'') (72 Original Hits) Cd 5", "01/01/1980", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1980] Original Hits 80s 12 72 Original Hits Cd 5")]
        [InlineData("Vices & Virtues", "01/01/1973", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1973] Vices And Virtues")]
        [InlineData("Pretty, Odd.", "01/01/1973", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1973] Pretty, Odd.")]
        [InlineData(" !A Pretty Odd   Day", "01/01/1973", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1973] A Pretty Odd Day")]
        [InlineData("A.Pretty~Odd=Day", "01/01/1973", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1973] A Pretty Odd Day")]
        [InlineData("A", "01/01/1973", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1973] A")]
        [InlineData("X", "01/01/1973", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1973] X")]
        [InlineData("Done-Over", "01/01/1973", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1973] Done Over")]
        [InlineData("%", "01/01/1973", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1973] Per")]
        [InlineData("The Blues Brothers: Music From The Soundtrack", "01/01/1973", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1973] The Blues Brothers Music From The Soundtrack")]
        [InlineData("14", "01/01/1973", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1973] 14")]
        [InlineData(@"12\18\2103", "01/01/1973", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1973] 12182103")]
        [InlineData(@"Insect Warfare / The Kill", "01/01/1973", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1973] Insect Warfare The Kill")]
        [InlineData("Live (1955)", "01/01/1973", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1973] Live 1955")]
        [InlineData("We're Only In It For The Money", "01/01/1968", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1968] Were Only In It For The Money")]
        [InlineData("Rock \"N Roll High School", "01/01/1968", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1968] Rock N Roll High School")]
        [InlineData("1958-1959 Dr. Jekyll (With Cannonball Adderley)", "01/01/1968", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1968] 1958 1959 Dr Jekyll With Cannonball Adderley")]
        [InlineData("Hello, Dolly!", "01/01/1968", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1968] Hello, Dolly")]
        [InlineData("Deäth", "01/01/1981", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1981] Death")]
        [InlineData("Sinatra's Swingin' Session!!! And More", "01/01/1981", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1981] Sinatras Swingin Session And More")]
        [InlineData("01234567890123456789012345678901234567890123456789", "01/01/1974", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1974] 01234567890123456789012345678901234567890123456789")]
        [InlineData("At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint occaecati cupiditate non providenta", "01/01/1975", @"I\IR\Iron Maiden [9909]", @"I\IR\Iron Maiden [9909]\[1975] At Vero Eos Et Accusamus Et Iusto Odio Dignissimos Ducimus Qui Blanditiis Praesentium Volupta")]
        public void GenerateReleaseFolderNames(string input, string releaseDate, string artistFolder, string shouldBe)
        {
            var af = new DirectoryInfo(Path.Combine(Configuration.LibraryFolder, artistFolder));
            var releaseFolder = FolderPathHelper.ReleasePath(af.FullName, input, SafeParser.ToDateTime(releaseDate).Value);
            var t = new DirectoryInfo(Path.Combine(Configuration.LibraryFolder, shouldBe));
            Assert.Equal(t.FullName, releaseFolder);
        }
    }
}