using Roadie.Api.Services;
using Roadie.Library.Identity;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace Roadie.Library.Tests
{
    public class TokenHelperTests
    {
        [Fact]
        public void GeneratePlayToken()
        {
            var sw = Stopwatch.StartNew();

            var token = ServiceBase.TrackPlayToken(new User
            {
                Id = 1,
                CreatedDate = DateTime.UtcNow
            }, Guid.NewGuid());
            sw.Stop();

            Assert.NotNull(token);
        }
    }
}
