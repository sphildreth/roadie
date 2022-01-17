using Microsoft.Extensions.Configuration;
using Roadie.Library.Configuration;
using Xunit;

namespace Roadie.Library.Tests
{
    public class ConfigurationTests
    {
        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
            return config;
        }

        private readonly IRoadieSettings _settings;

        private readonly IConfiguration _configuration;

        private IRoadieSettings Settings
        {
            get
            {
                return _settings;
            }
        }

        public ConfigurationTests()
        {
            _configuration = InitConfiguration();
            _settings = new RoadieSettings();
            _configuration.GetSection("RoadieSettings").Bind(_settings);
        }

        [Fact]
        public void LoadRootLevelConfiguration()
        {
            var inboundFolder = @"C:\roadie_dev_root\inbound";
            var configInboundFolder = Settings.InboundFolder;
            Assert.Equal(inboundFolder, configInboundFolder);
        }
    }
}