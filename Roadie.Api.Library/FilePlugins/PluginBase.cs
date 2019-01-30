using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Factories;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Roadie.Library.FilePlugins
{
    public abstract class PluginBase : IFilePlugin
    {
        public abstract string[] HandlesTypes { get; }

        public int MinWeightToDelete
        {
            get
            {
                return this.Configuration.FilePlugins.MinWeightToDelete;
            }
        }

        protected IArtistFactory ArtistFactory { get; }


        protected ICacheManager CacheManager { get; }

        protected IRoadieSettings Configuration { get; }

        protected IHttpEncoder HttpEncoder { get; }

        protected IImageFactory ImageFactory { get; }

        protected ILogger Logger { get; }

        protected IReleaseFactory ReleaseFactory { get; }

        protected IArtistLookupEngine ArtistLookupEngine { get; }
        protected IReleaseLookupEngine ReleaseLookupEngine { get; }

        public PluginBase(IRoadieSettings configuration, IHttpEncoder httpEncoder, IArtistFactory artistFactory, IReleaseFactory releaseFactory, IImageFactory imageFactory, ICacheManager cacheManager, ILogger logger, IArtistLookupEngine artistLookupEngine, IReleaseLookupEngine releaseLookupEngine)
        {
            this.Configuration = configuration;
            this.HttpEncoder = httpEncoder;
            this.ArtistFactory = artistFactory;
            this.ReleaseFactory = releaseFactory;
            this.ImageFactory = imageFactory;
            this.CacheManager = cacheManager;
            this.Logger = logger;
            this.ArtistLookupEngine = artistLookupEngine;
            this.ReleaseLookupEngine = releaseLookupEngine;

        }

        /// <summary>
        /// Check if exists if not make given folder
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

        public abstract Task<OperationResult<bool>> Process(string destinationRoot, FileInfo fileInfo, bool doJustInfo, int? submissionId);

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
                if (stream != null)
                {
                    stream.Close();
                }
            }

            //file is not locked
            return false;
        }
    }
}