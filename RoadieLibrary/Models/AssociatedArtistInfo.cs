using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Roadie.Data.Models
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
