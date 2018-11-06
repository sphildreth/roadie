using Roadie.Library.Configuration;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{
    [DataContract]
    public class License
    {
        [DataMember(Name = "valid")]
        [XmlAttribute(AttributeName = "valid")]
        public bool valid { get; set; }
        [DataMember(Name = "email")]
        [XmlAttribute(AttributeName = "email")]
        public string email { get; set; }
        [DataMember(Name = "key")]
        [XmlAttribute(AttributeName = "key")]
        public string key { get; set; }
        [DataMember(Name = "licenseExpires")]
        [XmlAttribute(AttributeName = "licenseExpires")]
        public string licenseExpires { get; set; }

        public License(IRoadieSettings configuration)
        {
            this.valid = true;
            this.email = configuration.SmtpFromAddress;
            this.key = "C617BEA251B9E2C03DCF7289";
            this.licenseExpires = DateTime.UtcNow.AddYears(5).ToString("s");
        }
    }
}
