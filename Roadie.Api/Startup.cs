using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Roadie.Api.Hubs;
using Roadie.Api.ModelBinding;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Identity;
using Roadie.Library.Imaging;
using Roadie.Library.Utility;
using System;

namespace Roadie.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;

        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            this._configuration = configuration;
            this._loggerFactory = loggerFactory;

            TypeAdapterConfig<Roadie.Library.Data.Image, Roadie.Library.Models.Image>
                .NewConfig()
                .Map(i => i.ArtistId,
                     src => src.Artist == null ? null : (Guid?)src.Artist.RoadieId)
                .Map(i => i.ReleaseId,
                     src => src.Release == null ? null : (Guid?)src.Release.RoadieId)
                .Compile();

            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("CORSPolicy");

            app.UseAuthentication();
            //app.UseSwagger();
            //app.UseSwaggerUI(c =>
            //{
            //    c.SwaggerEndpoint("/swagger/swagger.json", "Roadie API");
            //    c.RoutePrefix = string.Empty;
            //});

            app.UseStaticFiles();

            app.UseSignalR(routes =>
            {
                routes.MapHub<PlayActivityHub>("/playActivityHub");
                routes.MapHub<ScanActivityHub>("/scanActivityHub");
            });
            app.UseMvc();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options => options.AddPolicy("CORSPolicy", builder =>
            {
                builder
                .WithOrigins("http://localhost:8080", "https://localhost:8080", "http://localhost:80", "https://localhost:80")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
            }));

            services.AddSingleton<ITokenService, TokenService>();

            services.AddSingleton<IHttpEncoder, HttpEncoder>();

            var cacheManager = new DictionaryCacheManager(this._loggerFactory.CreateLogger<DictionaryCacheManager>(), new CachePolicy(TimeSpan.FromHours(4)));
            services.AddSingleton<ICacheManager>(cacheManager);

            services.AddDbContextPool<ApplicationUserDbContext>(
                options => options.UseMySql(this._configuration.GetConnectionString("RoadieDatabaseConnection"),
                mySqlOptions =>
                {
                    mySqlOptions.ServerVersion(new Version(5, 5), Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MariaDb);
                }
            ));

            services.AddDbContextPool<IRoadieDbContext, RoadieDbContext>(
                options => options.UseMySql(this._configuration.GetConnectionString("RoadieDatabaseConnection"),
                mySqlOptions =>
                {
                    mySqlOptions.ServerVersion(new Version(5, 5), Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MariaDb);
                }
            ));

            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationUserDbContext>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
                options.AddPolicy("Editor", policy => policy.RequireRole("Admin", "Editor"));
            });

            services.Configure<IConfiguration>(this._configuration);

            services.AddSingleton<IRoadieSettings, RoadieSettings>(ctx =>
            {
                var settings = new RoadieSettings();
                var configuration = ctx.GetService<IConfiguration>();
                configuration.GetSection("RoadieSettings").Bind(settings);
                var hostingEnvironment = ctx.GetService<IHostingEnvironment>();
                settings.ContentPath = hostingEnvironment.WebRootPath;
                settings.ConnectionString = this._configuration.GetConnectionString("RoadieDatabaseConnection");

                var integrationKeys = this._configuration.GetSection("IntegrationKeys")
                                                         .Get<IntegrationKey>();

                settings.Integrations.ApiKeys = new System.Collections.Generic.List<ApiKey>
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
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<ICollectionService, CollectionService>();
            services.AddScoped<IPlaylistService, PlaylistService>();
            services.AddScoped<IBookmarkService, BookmarkService>();
            services.AddScoped<IArtistService, ArtistService>();
            services.AddScoped<IImageService, ImageService>();
            services.AddScoped<IReleaseService, ReleaseService>();
            services.AddScoped<ITrackService, TrackService>();
            services.AddScoped<ILabelService, LabelService>();
            services.AddScoped<IPlaylistService, PlaylistService>();
            services.AddScoped<IPlayActivityService, PlayActivityService>();
            services.AddScoped<IGenreService, GenreService>();
            services.AddScoped<ISubsonicService, SubsonicService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<ILookupService, LookupService>();

            var securityKey = new SymmetricSecurityKey(System.Text.Encoding.Default.GetBytes(this._configuration["Tokens:PrivateKey"]));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(config =>
            {
                config.RequireHttpsMetadata = false;
                config.SaveToken = true;
                config.TokenValidationParameters = new TokenValidationParameters()
                {
                    IssuerSigningKey = securityKey,

                    ValidateAudience = true,
                    ValidAudience = this._configuration["Tokens:Audience"],
                    ValidateIssuer = true,
                    ValidIssuer = this._configuration["Tokens:Issuer"],
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
            .AddJsonOptions(options =>
            {
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            })
            .AddXmlSerializerFormatters()
            .SetCompatibilityVersion(CompatibilityVersion.Latest);

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
            services.AddScoped<IHttpContext>(factory =>
            {
                var actionContext = factory.GetService<IActionContextAccessor>()
                                           .ActionContext;
                return new HttpContext(new UrlHelper(actionContext));
            });
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