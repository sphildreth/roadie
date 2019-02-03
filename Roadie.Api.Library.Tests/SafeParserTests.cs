﻿using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Roadie.Library.Tests
{
    public class SafeParserTests
    {
        [Theory]
        [InlineData("1")]
        [InlineData("01")]
        [InlineData("001")]
        public void Parse_Int_ShouldBeOne(string input)
        {
            var parsed = SafeParser.ToNumber<int>(input);
            Assert.Equal(1, parsed);
        }

        [Theory]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("00")]
        public void Parse_Int_ShouldBeZero(string input)
        {
            var parsed = SafeParser.ToNumber<int>(input);
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
        [InlineData("02/22/1988")]
        [InlineData("02/22/88")]
        [InlineData("02-22-1988")]
        [InlineData("02--22--1988")]
        [InlineData("02-22--1988")]
        [InlineData("02--22-1988")]
        [InlineData("02-22-88")]
        [InlineData("04/1988")]
        [InlineData("04//1988")]
        [InlineData("04///1988")]
        [InlineData("04-1988")]
        [InlineData("04\\1988")]
        [InlineData("04\\\\1988")]
        [InlineData("1988")]
        [InlineData("1988/05")]
        [InlineData("1988/05/02")]
        [InlineData("88")]
        public void Parse_Datetime_ShouldBe1988(string input)
        {
            var parsed = SafeParser.ToDateTime(input);
            Assert.NotNull(parsed);
            Assert.Equal(1988, parsed.Value.Year);
        }

        [Theory]
        [InlineData("2004//2004")]
        [InlineData("2004/2004")]
        [InlineData("2004-2004")]
        [InlineData("2004--2004")]
        [InlineData("2004////2004")]
        [InlineData("2004\\2004")]
        [InlineData("2004\\\\2004")]
        public void Parse_Datetime_ShouldBe2004(string input)
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

        [Theory]
        [InlineData("true")]
        [InlineData("True")]
        [InlineData("TRUE")]
        [InlineData("Y")]
        [InlineData("Yes")]
        [InlineData("YES")]
        [InlineData("1")]
        public void Parse_Boolean_ShouldBeTrue(string input)
        {
            var parsed = SafeParser.ToBoolean(input);
            Assert.True(parsed);
        }


        [Theory]
        [InlineData("")]
        [InlineData("BATMAN")]
        [InlineData("false")]
        [InlineData("False")]
        [InlineData("FALSE")]
        [InlineData("N")]
        [InlineData("No")]
        [InlineData("NO")]
        [InlineData("0")]
        public void Parse_Boolean_ShouldBeFalse(string input)
        {
            var parsed = SafeParser.ToBoolean(input);
            Assert.False(parsed);
        }
    }
}
