﻿using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Roadie.Library.Tests
{
    public class SafeParserTests
    {
        [Fact]
        public void Parse_Int()
        {
            var one = "1";
            int n = 1;
            var parsed = SafeParser.ToNumber<int>(one);
            Assert.Equal(parsed, n);

            var nothing = "";
            parsed = SafeParser.ToNumber<int>(nothing);
            Assert.Equal(0, parsed);
        }

        [Fact]
        public void Parse_Larger_Int()
        {
            var n = "12345";
            int n1 = 12345;
            var parsed = SafeParser.ToNumber<int>(n);
            Assert.Equal(parsed, n1);
        }

        [Fact]
        public void Parse_NullablleInt()
        {
            var one = "1";
            var parsed = SafeParser.ToNumber<int?>(one);
            Assert.True(parsed.HasValue);

            int? oneNullable = 1;
            Assert.Equal(parsed.Value, oneNullable);

            var nothing = "";
            parsed = SafeParser.ToNumber<int?>(nothing);
            Assert.Null(parsed);

        }

        [Fact]
        public void Parse_Short()
        {
            var n = "23";
            short n1 = 23;
            var parsed = SafeParser.ToNumber<short>(n);
            Assert.Equal(parsed, n1);
        }

        [Theory]
        [InlineData("02-22-88")]
        [InlineData("02/22/88")]
        [InlineData("2004")]
        [InlineData("88")]
        [InlineData("04/2015")]
        [InlineData("2015/05")]
        public void Parse_Datetime(string input)
        {
            var parsed = SafeParser.ToDateTime(input);
            Assert.NotNull(parsed);
        }

        [Theory]
        [InlineData("DEB4F298-5D22-4304-916E-F130B02864B7")]
        [InlineData("12d65c61-1b7d-4c43-9aab-7d398a1a880e")]
        [InlineData("A:8a951bc1-5ee5-4961-b72a-99d91d84c147")]
        [InlineData("R:0327eea7-b1cb-4ae9-9eb1-b74b4416aefb")]
        public void Parse_Guid(string input)
        {
            var parsed = SafeParser.ToGuid(input);
            Assert.NotNull(parsed);
        }


    }
}