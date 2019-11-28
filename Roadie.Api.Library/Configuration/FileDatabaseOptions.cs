using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Configuration
{
    /// <summary>
    /// Options specific when using a FileDatabase DbContext.
    /// </summary>
    public sealed class FileDatabaseOptions : IFileDatabaseOptions
    {
        public FileDatabaseFormat DatabaseFormat { get; set; }

        public string DatabaseName { get; set; }

        public string DatabaseFolder { get; set; }

        public FileDatabaseOptions()
        {
            DatabaseFormat = FileDatabaseFormat.BSON;
            DatabaseName = "roadie";
            DatabaseFolder = "data/db";
        }
    }
}
