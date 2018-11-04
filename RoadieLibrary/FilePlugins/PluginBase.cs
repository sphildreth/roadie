using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Factories;
using Roadie.Library.Logging;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Roadie.Library.FilePlugins
{
    public abstract class PluginBase : IFilePlugin
    {
        protected readonly ArtistFactory _artistFactory = null;
        protected readonly ICacheManager _cacheManager = null;
        protected readonly IRoadieSettings _configuration = null;
        protected readonly IHttpEncoder _httpEncoder = null;
        protected readonly ImageFactory _imageFactory = null;
        protected readonly ILogger _loggingService = null;
        protected readonly ReleaseFactory _releaseFactory = null;
        protected Audio _audioPlugin = null;
        protected ID3TagsHelper _id3TagsHelper = null;
        public abstract string[] HandlesTypes { get; }

        public int MinWeightToDelete
        {
            get
            {
                return this.Configuration.FilePlugins.MinWeightToDelete;
            }
        }

        protected ArtistFactory ArtistFactory
        {
            get
            {
                return this._artistFactory;
            }
        }

        protected Audio AudioPlugin
        {
            get
            {
                return this._audioPlugin ?? (this._audioPlugin = new Audio(this.Configuration, this.HttpEncoder, this.ArtistFactory, this.ReleaseFactory, this.ImageFactory, this.CacheManager, this.Logger));
            }
            set
            {
                this._audioPlugin = value;
            }
        }

        protected ICacheManager CacheManager
        {
            get
            {
                return this._cacheManager;
            }
        }

        protected IRoadieSettings Configuration
        {
            get
            {
                return this._configuration;
            }
        }

        protected IHttpEncoder HttpEncoder
        {
            get
            {
                return this._httpEncoder;
            }
        }

        protected ID3TagsHelper ID3TagsHelper
        {
            get
            {
                return this._id3TagsHelper ?? (this._id3TagsHelper = new ID3TagsHelper(this.Configuration, this.CacheManager, this.Logger));
            }
            set
            {
                this._id3TagsHelper = value;
            }
        }

        protected ImageFactory ImageFactory
        {
            get
            {
                return this._imageFactory;
            }
        }

        protected ILogger Logger
        {
            get
            {
                return this._loggingService;
            }
        }

        protected ReleaseFactory ReleaseFactory
        {
            get
            {
                return this._releaseFactory;
            }
        }

        public PluginBase(IRoadieSettings configuration, IHttpEncoder httpEncoder, ArtistFactory artistFactory, ReleaseFactory releaseFactory, ImageFactory imageFactory, ICacheManager cacheManager, ILogger logger)
        {
            this._configuration = configuration;
            this._httpEncoder = httpEncoder;
            this._artistFactory = artistFactory;
            this._releaseFactory = releaseFactory;
            this._imageFactory = imageFactory;
            this._cacheManager = cacheManager;
            this._loggingService = logger;
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
                Trace.WriteLine(string.Format("Created Directory [{0}]", folder));
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