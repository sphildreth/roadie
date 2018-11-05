using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Roadie.Api.Data.Models
{
    [Serializable]
    public class AssociatedArtistInfo : EntityInfoModelBase
    {
        public string Name { get; set; }

        public string Tooltip
        {
            get
            {
                return this.Name;
            }
        }
    }
}
