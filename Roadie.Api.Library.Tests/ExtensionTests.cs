using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.Processors;
using Roadie.Library.SearchEngines.MetaData.Discogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Roadie.Library.Tests
{
    public class ExtensionTests
    {

        [Fact]
        public void ShuffleUniqueOrder()
        {
            var tracks = new List<Roadie.Library.Models.TrackList>
            {
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value ="A1_R1", Text = "A1_R1" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A1", Text = "A1" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R1", Text = "R1" }
                    }                    
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value = "A2_R4", Text = "A2_R4" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A2", Text = "A2" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R4", Text = "R4" }
                    }
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value = "A1_R5", Text = "A1_R5" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A1", Text = "A1" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R5", Text = "R5" }
                    }
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value = "A1_R1", Text = "A1_R1" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A1", Text = "A1" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R1", Text = "R1" }
                    }
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value = "A3_R6", Text = "A3_R6" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A3", Text = "A3" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R6", Text = "R6" }
                    }
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value = "A2_R5_1", Text = "A2_R5_1" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A2", Text = "A2" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R5", Text = "R5" }
                    }
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value = "A2_R5_2", Text = "A2_R5_2" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A2", Text = "A2" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R5", Text = "R5" }
                    }
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value = "A2_R5_3", Text = "A2_R5_3" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A2", Text = "A2" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R5", Text = "R5" }
                    }
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value = "A2_R6", Text = "A2_R6" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A2", Text = "A2" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R6", Text = "R6" }
                    }
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value = "A4_R7", Text = "A4_R7" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A4", Text = "A4" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R7", Text = "R7" }
                    }
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value = "A5_R8", Text = "A5_R8" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A5", Text = "A5" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R8", Text = "R8" }
                    }
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value = "A6_R9", Text = "A6_R9" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A6", Text = "A6" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R9", Text = "R9" }
                    }
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value = "A7_R10", Text = "A7_R10" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A7", Text = "A7" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R10", Text = "R10" }
                    }
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value = "A8_R11", Text = "A8_R11" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A8", Text = "A8" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R11", Text = "R11" }
                    }
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value ="A9_R12", Text = "A9_R12" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A9", Text = "A9" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R12", Text = "R12" }
                    }
                },
                new Models.TrackList
                {
                    Track = new Models.DataToken { Value ="A10_R13", Text = "A10_R13" },
                    Artist = new Models.ArtistList
                    {
                        Artist = new Models.DataToken { Value = "A10", Text = "A10" }
                    },
                    Release = new Models.Releases.ReleaseList
                    {
                        Release = new Models.DataToken { Value = "R13", Text = "R13" }
                    }
                }
            };

            var shuffledTracks = Models.TrackList.Shuffle(tracks);
            var lastTrack = shuffledTracks.First();
            foreach(var track in shuffledTracks.Skip(1))
            {
                Assert.False(string.Equals(track.Artist.Artist.Text, lastTrack.Artist.Artist.Text, StringComparison.Ordinal) &&
                             string.Equals(track.Release.Release.Text, lastTrack.Release.Release.Text, StringComparison.Ordinal));
                lastTrack = track;
            }

            Assert.Equal(tracks.Count, shuffledTracks.Count());
        }


        [Fact]
        public void FromUnixTime()
        {
            var dateTime = new DateTime(2015, 05, 24, 10, 2, 0, DateTimeKind.Utc);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var unixDateTime = (long)(dateTime.ToUniversalTime() - epoch).TotalSeconds;
            var ts = unixDateTime.FromUnixTime();
            Assert.Equal(dateTime, ts);
        }

        [Fact]
        public void ToUnixTime()
        {
            var dateTime = new DateTime(2015, 05, 24, 10, 2, 0, DateTimeKind.Utc);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var unixDateTime = (long)(dateTime.ToUniversalTime() - epoch).TotalSeconds;
            var unixTime = dateTime.ToUnixTime();
            Assert.Equal(unixDateTime, unixTime);
        }

        [Theory]
        [InlineData(3000, 3)]
        [InlineData(150, 0)]
        [InlineData(65000, 65)]
        [InlineData(143000, 143)]
        public void ToSecondsFromMillisecondsDecimal(int input, int shouldBe)
        {
            var d = ((decimal?)input).ToSecondsFromMilliseconds();
            Assert.Equal(shouldBe, d);
        }

        [Fact]
        public void ToTimeSpan()
        {
            var dateTime = new DateTime(2015, 05, 24, 10, 2, 0, DateTimeKind.Utc);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var shouldBe = (dateTime.ToUniversalTime() - epoch);
            decimal? unixDateTime = (long)(shouldBe).TotalSeconds;
            var ts = unixDateTime.ToTimeSpan();
            Assert.Equal(shouldBe, ts);
            
        }

        [Fact]
        public void OrIntegers()
        {
            int? test = null;
            int? shouldBe = 5;
            Assert.Equal(shouldBe, test.Or(5));
        }

        [Theory]
        [InlineData(3000, 3)]
        [InlineData(150, 0)]
        [InlineData(65000, 65)]
        [InlineData(143000, 143)]
        public void ToSecondsFromMillisecondsInt(int? input, int? shouldBe)
        {
            var d = input.ToSecondsFromMilliseconds();
            Assert.Equal(shouldBe, d);
        }
    }
}
