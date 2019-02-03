using Microsoft.Extensions.Configuration;
using Roadie.Library.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private readonly IRoadieSettings _settings = null;

        private readonly IConfiguration _configuration;
        private IConfiguration Configuration
        {
            get
            {
                return this._configuration;
            }
        }

        private IRoadieSettings Settings
        {
            get
            {
                return this._settings;
            }
        }

        public ConfigurationTests()
        {
            this._configuration = InitConfiguration();
            this._settings = new RoadieSettings();
            this._configuration.GetSection("RoadieSettings").Bind(this._settings);
        }

        [Fact]
        public void Load_Root_Level_Configuration()
        {
            var inboundFolder = @"C:\roadie_dev_root\inbound";
            var configInboundFolder = this.Settings.InboundFolder;
            Assert.Equal(inboundFolder, configInboundFolder);
        }



    }

}
