using System;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public class Converting : IConverting
    {
        public string APEConvertCommand { get; set; }
        public bool ConvertingEnabled { get; set; }
        public bool DoDeleteAfter { get; set; }
        public string M4AConvertCommand { get; set; }
        public string OGGConvertCommand { get; set; }
        public string FLACConvertCommand { get; set; }
    }
}