using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Dlna.Server;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Imaging;
using Roadie.Library.Scrobble;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Dlna.Services
{
    /// <summary>
    /// Hosted Service for Dlna Service (not the actual Dlna Service)
    /// </summary>
    public class DlnaHostService : IHostedService, IDisposable
    {
        private HttpAuthorizer _authorizer = null;

        private ICacheManager CacheManager { get; }
        private IRoadieSettings Configuration { get; }
        private IRoadieDbContext DbContext { get; set; }
        private ILogger Logger { get; }
        private ILoggerFactory LoggerFactory { get; }
        private IServiceScopeFactory ServiceScopeFactory { get; }

        public DlnaHostService(IServiceScopeFactory serviceScopeFactory, IRoadieSettings configuration, ICacheManager cacheManager,
                               ILoggerFactory loggerFactory)
        {
            ServiceScopeFactory = serviceScopeFactory;
            Configuration = configuration;
            CacheManager = cacheManager;
            LoggerFactory = loggerFactory;
            Logger = loggerFactory.CreateLogger("DlnaHostService");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!Configuration.Dlna.IsEnabled)
            {
                Logger.LogInformation("DLNA service disabled.");
                return;
            }
            var server = new HttpServer(LoggerFactory.CreateLogger("HttpServer"), Configuration.Dlna.Port ?? 0);
            _authorizer = new HttpAuthorizer(server);
            if (Configuration.Dlna.AllowedIps.Any())
            {
                _authorizer.AddMethod(new IPAddressAuthorizer(Configuration.Dlna.AllowedIps));
            }
            if (Configuration.Dlna.AllowedUserAgents.Any())
            {
                _authorizer.AddMethod(new UserAgentAuthorizer(Configuration.Dlna.AllowedUserAgents));
            }
            DbContext = DbContextFactory.Create(Configuration);

            var defaultNotFoundImages = new DefaultNotFoundImages(LoggerFactory.CreateLogger("DefaultNotFoundImages"), Configuration);
            var imageService = new ImageService(Configuration, DbContext, CacheManager, LoggerFactory.CreateLogger("ImageService"), defaultNotFoundImages);
            var trackService = new TrackService(Configuration, DbContext, CacheManager, LoggerFactory.CreateLogger("TrackService"));
            var roadieScrobbler = new RoadieScrobbler(Configuration, LoggerFactory.CreateLogger("RoadieScrobbler"), DbContext, CacheManager);
            var scrobbleHandler = new ScrobbleHandler(Configuration, LoggerFactory.CreateLogger("ScrobbleHandler"), DbContext, CacheManager, roadieScrobbler);
            var playActivityService = new PlayActivityService(Configuration, DbContext, CacheManager, LoggerFactory.CreateLogger("PlayActivityService"), scrobbleHandler);

            var rs = new DlnaService(Configuration, DbContext, CacheManager, LoggerFactory.CreateLogger("DlnaService"), imageService, trackService, playActivityService);
            rs.Preload();

            server.RegisterMediaServer(Configuration, LoggerFactory.CreateLogger("MediaMount"), rs);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(5000, cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_authorizer != null)
                    {
                        _authorizer.Dispose();
                    }
                    if (DbContext != null)
                    {
                        DbContext.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DlnaService()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        #endregion IDisposable Support
    }
}