using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.ID3Tags;
using System.Collections.Generic;
using Xunit;

namespace Roadie.Library.Tests
{
    public class RenumberTests
    {
        [Theory]
        [InlineData(@"2003 - Accelerated Evolution\01 - Depth Charge.mp3")]
        [InlineData(@"01 - Depth Charge.mp3")]
        [InlineData(@"2003 - Accelerated Evolution\CD1\01 - Depth Charge.mp3")]
        [InlineData(@"2003 - Accelerated Evolution\CD01\01 - Depth Charge.mp3")]
        [InlineData(@"2003 - Accelerated Evolution\CD001\01 - Depth Charge.mp3")]
        [InlineData(@"CD 1\01 - Depth Charge.mp3")]
        [InlineData(@"2003 - Accelerated Evolution\CD 01\01 - Depth Charge.mp3")]
        [InlineData(@"2003 - Accelerated Evolution\CD 001\01 - Depth Charge.mp3")]
        [InlineData(@"Accelerated Evolution CD01\22 - Depth Charge.mp3")]
        [InlineData(@"Accelerated Evolution CD1\22 - Depth Charge.mp3")]
        public void FindDiscNumberShouldBeOne(string filename)
        {
            var n = ID3TagsHelper.DetermineDiscNumber(new AudioMetaData { Filename = filename });
            Assert.Equal(1, n);
        }

        [Theory]
        [InlineData(@"2003 - Accelerated Evolution\CD2\01 - Depth Charge.mp3")]
        [InlineData(@"2003 - Accelerated Evolution\CD02\01 - Depth Charge.mp3")]
        [InlineData(@"2003 - Accelerated Evolution\CD002\01 - Depth Charge.mp3")]
        [InlineData(@"2003 - Accelerated Evolution\CD 2\02 - Depth Charge.mp3")]
        [InlineData(@"2003 - Accelerated Evolution\CD 02\01 - Depth Charge.mp3")]
        [InlineData(@"2003 - Accelerated Evolution\CD 002\22 - Depth Charge.mp3")]
        [InlineData(@"Accelerated Evolution CD2\22 - Depth Charge.mp3")]
        [InlineData(@"Accelerated Evolution CD02\22 - Depth Charge.mp3")]
        public void FindDiscNumberShouldBeTwo(string filename)
        {
            var n = ID3TagsHelper.DetermineDiscNumber(new AudioMetaData { Filename = filename });
            Assert.Equal(2, n);
        }

        [Fact]
        public void FindTotalDiscsShouldBeOne()
        {
            var three = new List<AudioMetaData>
            {
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\01 - Depth Charge.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\02 - Not A Depth Charge.mp3"
                }
            };
            var n = ID3TagsHelper.DetermineTotalDiscNumbers(three);
            Assert.Equal(1, n);

            three = new List<AudioMetaData>
            {
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD1\01 - Depth Charge.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD1\02 - Not A Depth Charge.mp3"
                }
            };
            n = ID3TagsHelper.DetermineTotalDiscNumbers(three);
            Assert.Equal(1, n);
        }

        [Fact]
        public void FindTotalDiscsShouldBeTen()
        {
            var three = new List<AudioMetaData>
            {
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 01\01 - Depth Charge.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 02\02 - Not A Depth Charge.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 04\01 - First.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 06\02 - Second.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 10\01 - Depth Charge.mp3"
                }
            };
            var n = ID3TagsHelper.DetermineTotalDiscNumbers(three);
            Assert.Equal(10, n);
        }

        [Fact]
        public void FindTotalDiscsShouldBeThree()
        {
            var three = new List<AudioMetaData>
            {
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 01\01 - Depth Charge.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 01\02 - Not A Depth Charge.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 02\01 - First.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 02\02 - Second.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 03\01 - Depth Charge.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 03\02 - Depth Charge.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 03\03 - Depth Charge.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 03\04 - Depth Charge.mp3"
                }
            };
            var n = ID3TagsHelper.DetermineTotalDiscNumbers(three);
            Assert.Equal(3, n);
        }

        [Fact]
        public void FindTotalDiscsShouldBeTwo()
        {
            var three = new List<AudioMetaData>
            {
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 01\01 - Depth Charge.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 01\02 - Not A Depth Charge.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 01\01 - First.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 02\02 - Second.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 01\01 - Depth Charge.mp3"
                }
            };
            var n = ID3TagsHelper.DetermineTotalDiscNumbers(three);
            Assert.Equal(2, n);

            three = new List<AudioMetaData>
            {
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD0\01 - Depth Charge.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD2\02 - Not A Depth Charge.mp3"
                }
            };
            n = ID3TagsHelper.DetermineTotalDiscNumbers(three);
            Assert.Equal(2, n);

            three = new List<AudioMetaData>
            {
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 1\01 - Depth Charge.mp3"
                },
                new AudioMetaData
                {
                    Filename = @"C:\roadiedevroot\Devin Townsend (1996 - 2016)\The Devin Townsend Band (2003 - 2006)\2003 - Accelerated Evolution\CD 2\02 - Not A Depth Charge.mp3"
                }
            };
            n = ID3TagsHelper.DetermineTotalDiscNumbers(three);
            Assert.Equal(2, n);
        }
    }
}