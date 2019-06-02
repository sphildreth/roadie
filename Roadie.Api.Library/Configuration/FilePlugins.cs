using System;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public class FilePlugins : IFilePlugins
    {
        public int MinWeightToDelete { get; set; }
    }
}