using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.Processors;
using Roadie.Library.SearchEngines.MetaData.Discogs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Roadie.Library.Tests
{
    public class ExtensionTests
    {
        [Fact]
        public void From_Unix_Time()
        {
            var dateTime = new DateTime(2015, 05, 24, 10, 2, 0, DateTimeKind.Utc);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var unixDateTime = (long)(dateTime.ToUniversalTime() - epoch).TotalSeconds;
            var ts = unixDateTime.FromUnixTime();
            Assert.Equal(dateTime, ts);
        }

        [Fact]
        public void To_Unix_Time()
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
        public void To_Seconds_From_Milliseconds_Decimal(int input, int shouldBe)
        {
            var d = ((decimal?)input).ToSecondsFromMilliseconds();
            Assert.Equal(shouldBe, d);
        }

        [Fact]
        public void To_Time_Span()
        {
            var dateTime = new DateTime(2015, 05, 24, 10, 2, 0, DateTimeKind.Utc);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var shouldBe = (dateTime.ToUniversalTime() - epoch);
            decimal? unixDateTime = (long)(shouldBe).TotalSeconds;
            var ts = unixDateTime.ToTimeSpan();
            Assert.Equal(shouldBe, ts);
            
        }

        [Fact]
        public void Or_Integers()
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
        public void To_Seconds_From_Milliseconds_Int(int? input, int? shouldBe)
        {
            var d = input.ToSecondsFromMilliseconds();
            Assert.Equal(shouldBe, d);
        }
    }
}
