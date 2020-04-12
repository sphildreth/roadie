using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Utility;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Roadie.Library.FilePlugins
{
    public abstract class PluginBase : IFilePlugin
    {
        public abstract string[] HandlesTypes { get; }

        public int MinWeightToDelete => Configuration.FilePlugins.MinWeightToDelete;

        protected IArtistLookupEngine ArtistLookupEngine { get; }

        protected ICacheManager CacheManager { get; }

        protected IRoadieSettings Configuration { get; }

        protected IHttpEncoder HttpEncoder { get; }

        protected ILogger Logger { get; }

        protected IReleaseLookupEngine ReleaseLookupEngine { get; }

        public PluginBase(IRoadieSettings configuration, IHttpEncoder httpEncoder, ICacheManager cacheManager, ILogger logger,
            IArtistLookupEngine artistLookupEngine, IReleaseLookupEngine releaseLookupEngine)
        {
            Configuration = configuration;
            HttpEncoder = httpEncoder;
            CacheManager = cacheManager;
            Logger = logger;
            ArtistLookupEngine = artistLookupEngine;
            ReleaseLookupEngine = releaseLookupEngine;
        }

        /// <summary>
        ///     Check if exists if not make given folder
        /// </summary>
        /// <param name="folder">Folder To Check</param>
        /// <returns>False if Exists, True if Made</returns>
        public static bool CheckMakeFolder(string folder)
        {
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(folder), "Invalid Folder");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                return true;
            }

            return false;
        }

        public abstract Task<OperationResult<bool>> Process(FileInfo fileInfo, bool doJustInfo, int? submissionId);

        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null) stream.Close();
            }

            //file is not locked
            return false;
        }
    }
}