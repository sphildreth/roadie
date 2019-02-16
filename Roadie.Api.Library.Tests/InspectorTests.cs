using Roadie.Library.Inspect;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Roadie.Library.Tests
{
    public class InspectorTests
    {

        [Theory]
        [InlineData("Bob Jones")]
        [InlineData("Nancy Jones")]
        public void Generate_Inspector_Artist_Token(string artist)
        {
            var token = Inspector.ArtistInspectorToken(new MetaData.Audio.AudioMetaData { Artist = artist });
            Assert.NotNull(token);
        }

        [Fact]
        public void Should_Generate_Same_Token_Value()
        {
            var md = new MetaData.Audio.AudioMetaData { Artist = "Omniversum Fractum", Release = "Paradigm Of The Elementals Essence" };
            var artistToken = Inspector.ArtistInspectorToken(md);
            Assert.NotNull(artistToken);

            var releaseToken = Inspector.ReleaseInspectorToken(md);
            Assert.NotNull(releaseToken);
            Assert.NotEqual(artistToken, releaseToken);

            var secondReleaseToken = Inspector.ReleaseInspectorToken(md);
            Assert.NotNull(releaseToken);
            Assert.NotEqual(artistToken, releaseToken);
            Assert.Equal(secondReleaseToken, releaseToken);
        }

        [Fact]
        public void Generate_Inspector_Tokens_Artist_And_Release_Unique()
        {
            var md = new MetaData.Audio.AudioMetaData { Artist = "Bob Jones", Release = "Bob's First Release" };
            var artistToken = Inspector.ArtistInspectorToken(md);
            Assert.NotNull(artistToken);

            var releaseToken = Inspector.ReleaseInspectorToken(md);
            Assert.NotNull(releaseToken);

            Assert.NotEqual(artistToken, releaseToken);


        }
    }
}
