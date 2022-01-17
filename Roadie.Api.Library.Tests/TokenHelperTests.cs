using Roadie.Api.Services;
using Roadie.Library.Identity;
using System;
using System.Diagnostics;
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