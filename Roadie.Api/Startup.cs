#region Usings

using FileContextCore;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Roadie.Api.Hubs;
using Roadie.Api.ModelBinding;
using Roadie.Api.Services;
using Roadie.Dlna.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Data.Context.Implementation;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Identity;
using Roadie.Library.Imaging;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.FileName;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.MetaData.LastFm;
using Roadie.Library.MetaData.MusicBrainz;
using Roadie.Library.Processors;
using Roadie.Library.Scrobble;
using Roadie.Library.SearchEngines.Imaging;
using Roadie.Library.SearchEngines.MetaData.Discogs;
using Roadie.Library.SearchEngines.MetaData.Spotify;
using Roadie.Library.SearchEngines.MetaData.Wikipedia;
using Roadie.Library.Utility;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;

#endregion Usings

namespace Roadie.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseRouting();

            //app.UseSwagger();
            //app.UseSwaggerUI(c =>
            //{
            //    c.SwaggerEndpoint("/swagger/swagger.json", "Roadie API");
            //    c.RoutePrefix = string.Empty;
            //});

            app.Use((context, next) =>
            {
                context.Response.Headers["Roadie-Api-Version"] = RoadieApiVersion();
                return next.Invoke();
            });

            app.UseSerilogRequestLogging();

            app.UseCors("CORSPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<PlayActivityHub>("/playActivityHub");
                endpoints.MapHub<ScanActivityHub>("/scanActivityHub");
                endpoints.MapControllers();
                endpoints.MapDefaultControllerRoute();
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var settings = new RoadieSettings();
            _configuration.GetSection("RoadieSettings").Bind(settings);

            services.AddSingleton<ITokenService, TokenService>();
            services.AddSingleton<IHttpEncoder, HttpEncoder>();
            services.AddSingleton<IEmailSender, EmailSenderService>();

            services.AddSingleton<ICacheSerializer>(options =>
            {
                var logger = options.GetService<ILogger<Utf8JsonCacheSerializer>>();
                return new Utf8JsonCacheSerializer(logger);
            });

            services.AddSingleton<ICacheManager>(options =>
            {
                var logger = options.GetService<ILogger<MemoryCacheManager>>();
                var serializer = options.GetService<ICacheSerializer>();
                return new MemoryCacheManager(logger, serializer, new CachePolicy(TimeSpan.FromHours(4)));
            });

            var dbFolder = new DirectoryInfo(settings.FileDatabaseOptions.DatabaseFolder);

            var connectionString = _configuration.GetConnectionString("RoadieDatabaseConnection");
            switch (settings.DbContextToUse)
            {
                case DbContexts.MySQL:
                    services.AddDbContextPool<ApplicationUserDbContext>(
                        options => options.UseMySql(connectionString,
                            ServerVersion.AutoDetect(connectionString), mySqlOptions =>
                            {
                                mySqlOptions.EnableRetryOnFailure(
                                    10,
                                    TimeSpan.FromSeconds(30),
                                    null);
                            }
                        ));
                    services.AddDbContextPool<IRoadieDbContext, MySQLRoadieDbContext>(
                        options => options.UseMySql(connectionString,
                            ServerVersion.AutoDetect(connectionString), mySqlOptions =>
                            {
                                mySqlOptions.EnableRetryOnFailure(
                                    10,
                                    TimeSpan.FromSeconds(30),
                                    null);
                            }
                        ));
                    break;

                case DbContexts.SQLite:
                    if (!dbFolder.Exists)
                    {
                        dbFolder.Create();
                    }
                    var builder = new SqliteConnectionStringBuilder()
                    {
                        DataSource = Path.Combine(settings.FileDatabaseOptions.DatabaseFolder, $"{ settings.FileDatabaseOptions.DatabaseName }.db"),
                        Mode = SqliteOpenMode.ReadWriteCreate,
                        Cache = SqliteCacheMode.Shared
                    };
                    services.AddDbContext<ApplicationUserDbContext>(
                        options => options.UseSqlite(builder.ConnectionString)
                    );
                    services.AddDbContext<IRoadieDbContext, SQLiteRoadieDbContext>(
                        options => options.UseSqlite(builder.ConnectionString)
                    );
                    break;

                case DbContexts.File:
                    if (!dbFolder.Exists)
                    {
                        dbFolder.Create();
                    }
                    services.AddDbContext<ApplicationUserDbContext>(
                        options => options.UseFileContextDatabase(settings.FileDatabaseOptions.DatabaseFormat.ToString().ToLower(),
                                                                  databaseName: settings.FileDatabaseOptions.DatabaseName,
                                                                  location: settings.FileDatabaseOptions.DatabaseFolder)
                    );
                    services.AddDbContext<IRoadieDbContext, FileRoadieDbContext>(
                        options => options.UseFileContextDatabase(settings.FileDatabaseOptions.DatabaseFormat.ToString().ToLower(),
                                                                  databaseName: settings.FileDatabaseOptions.DatabaseName,
                                                                  location: settings.FileDatabaseOptions.DatabaseFolder)
                    );
                    break;

                default:
                    throw new NotImplementedException("Unknown DbContext Type");
            }

            services.AddIdentity<User, UserRole>()
                .AddRoles<UserRole>()
                .AddEntityFrameworkStores<ApplicationUserDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
                options.AddPolicy("Editor", policy => policy.RequireRole("Admin", "Editor"));
            });

            services.Configure<IConfiguration>(_configuration);
            var corsOrigins = (_configuration["CORSOrigins"] ?? "http://localhost:8080").Split('|');
            Trace.WriteLine($"Setting Up CORS Policy [{string.Join(", ", corsOrigins)}]");

            services.AddCors(options => options.AddPolicy("CORSPolicy", builder =>
            {
                builder
                    .WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }));

            services.AddSingleton<IRoadieSettings, RoadieSettings>(ctx =>
            {
                var settings = new RoadieSettings();
                var configuration = ctx.GetService<IConfiguration>();
                configuration.GetSection("RoadieSettings").Bind(settings);
                var hostingEnvironment = ctx.GetService<IWebHostEnvironment>();
                settings.ContentPath = hostingEnvironment.WebRootPath;
                settings.ConnectionString =  _configuration.GetConnectionString("RoadieDatabaseConnection");

                // This is so 'User Secrets' can be used in Debugging
                var integrationKeys = _configuration.GetSection("IntegrationKeys").Get<IntegrationKey>();
                if (integrationKeys != null)
                    settings.Integrations.ApiKeys = new List<ApiKey>
                    {
                        new ApiKey
                        {
                            ApiName = "LastFMApiKey",
                            Key = integrationKeys.LastFMApiKey,
                            KeySecret = integrationKeys.LastFMSecret
                        },
                        new ApiKey
                        {
                            ApiName = "DiscogsConsumerKey",
                            Key = integrationKeys.DiscogsConsumerKey,
                            KeySecret = integrationKeys.DiscogsConsumerSecret
                        },
                        new ApiKey
                        {
                            ApiName = "BingImageSearch",
                            Key = integrationKeys.BingImageSearch
                        }
                    };
                return settings;
            });

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<IDefaultNotFoundImages, DefaultNotFoundImages>();
            services.AddSingleton<IImageSearchManager, ImageSearchManager>();
            services.AddSingleton<IITunesSearchEngine, ITunesSearchEngine>();
            services.AddSingleton<IBingImageSearchEngine, BingImageSearchEngine>();
            services.AddSingleton<IMusicBrainzProvider, MusicBrainzProvider>();
            services.AddSingleton<ISpotifyHelper, SpotifyHelper>();
            services.AddSingleton<IDiscogsHelper, DiscogsHelper>();
            services.AddSingleton<IWikipediaHelper, WikipediaHelper>();
            services.AddSingleton<IFileNameHelper, FileNameHelper>();
            services.AddSingleton<IID3TagsHelper, ID3TagsHelper>();

            services.AddScoped<ILastFmHelper, LastFmHelper>();
            services.AddScoped<IRoadieScrobbler, RoadieScrobbler>();
            services.AddScoped<ILastFMScrobbler, LastFMScrobbler>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<ICollectionService, CollectionService>();
            services.AddScoped<IPlaylistService, PlaylistService>();
            services.AddScoped<IBookmarkService, BookmarkService>();
            services.AddScoped<IArtistLookupEngine, ArtistLookupEngine>();
            services.AddScoped<IReleaseLookupEngine, ReleaseLookupEngine>();
            services.AddScoped<ILabelLookupEngine, LabelLookupEngine>();
            services.AddScoped<IAudioMetaDataHelper, AudioMetaDataHelper>();
            services.AddScoped<IFileProcessor, FileProcessor>();
            services.AddScoped<IFileDirectoryProcessorService, FileDirectoryProcessorService>();
            services.AddScoped<IArtistService, ArtistService>();
            services.AddScoped<IImageService, ImageService>();
            services.AddScoped<IReleaseService, ReleaseService>();
            services.AddScoped<ITrackService, TrackService>();
            services.AddScoped<ILabelService, LabelService>();
            services.AddScoped<IPlaylistService, PlaylistService>();
            services.AddScoped<IPlayActivityService, PlayActivityService>();
            services.AddScoped<IScrobbleHandler, ScrobbleHandler>();
            services.AddScoped<IGenreService, GenreService>();
            services.AddScoped<ISubsonicService, SubsonicService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<ILookupService, LookupService>();
            services.AddScoped<ICommentService, CommentService>();

            services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, DlnaHostService>();

            var securityKey = new SymmetricSecurityKey(Encoding.Default.GetBytes(_configuration["Tokens:PrivateKey"]));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(config =>
            {
                config.RequireHttpsMetadata = false;
                config.SaveToken = true;
                config.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = securityKey,

                    ValidateAudience = true,
                    ValidAudience = _configuration["Tokens:Audience"],
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Tokens:Issuer"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
            });

            //services.AddSwaggerGen(c =>
            //{
            //    c.SwaggerDoc("v1", new Info
            //    {
            //        Title = "Roadie API",
            //        Version = "v1"
            //    });
            //});

            services.AddSignalR();

            services.AddMvc(options =>
                {
                    options.RespectBrowserAcceptHeader = true; // false by default
                    options.ModelBinderProviders.Insert(0, new SubsonicRequestBinderProvider());
                })
                .AddJsonOptions(options => options.JsonSerializerOptions.IgnoreNullValues = true)
                .AddXmlSerializerFormatters();

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;
            });

            services.AddHttpContextAccessor();

            services.AddHttpClient();

            services.AddScoped<IHttpContext>(factory =>
            {
                var actionContext = factory.GetService<IActionContextAccessor>().ActionContext;
                if (actionContext == null)
                {
                    return null;
                }
                return new HttpContext(factory.GetService<IRoadieSettings>(), new UrlHelper(actionContext));
            });

            var sp = services.BuildServiceProvider();
            var adminService = sp.GetService<IAdminService>();
            adminService.PerformStartUpTasks();
        }

        private static string _roadieApiVersion = null;

        public static string RoadieApiVersion()
        {
            if (string.IsNullOrEmpty(_roadieApiVersion))
            {
                _roadieApiVersion = typeof(Startup)
                                    .GetTypeInfo()
                                    .Assembly
                                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                    .InformationalVersion;
            }
            return _roadieApiVersion;
        }

        private class IntegrationKey
        {
            public string BingImageSearch { get; set; }
            public string DiscogsConsumerKey { get; set; }
            public string DiscogsConsumerSecret { get; set; }
            public string LastFMApiKey { get; set; }
            public string LastFMSecret { get; set; }
        }
    }
}