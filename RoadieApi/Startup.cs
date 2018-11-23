using Mapster;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using Newtonsoft.Json;
using Roadie.Api.Controllers;
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
using Serilog;
using System;
using models = Roadie.Library.Models;

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

            // global cors policy
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
            
            app.UseAuthentication();
            //app.UseSwagger();
            //app.UseSwaggerUI(c =>
            //{
            //    c.SwaggerEndpoint("/swagger/swagger.json", "Roadie API");
            //    c.RoutePrefix = string.Empty;
            //});
            app.UseSignalR(routes =>
            {
                routes.MapHub<PlayActivityHub>("/playActivityHub");
            });
            app.UseMvc(b =>
            {
                b.Select().Expand().Filter().OrderBy().MaxTop(100).Count();
                b.MapODataServiceRoute("odata", "odata", GetEdmModel());
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options => options.AddPolicy("Cors", builder =>
            {
                builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
            }));

            
            services.AddSingleton<ITokenService, TokenService>();

            services.AddSingleton<IHttpEncoder, HttpEncoder>();

            var cacheManager = new DictionaryCacheManager(this._loggerFactory.CreateLogger<DictionaryCacheManager>(), new CachePolicy(TimeSpan.FromHours(4)));
            services.AddSingleton<ICacheManager>(cacheManager);

            services.AddDbContextPool<ApplicationUserDbContext>(
                options => options.UseMySql(this._configuration.GetConnectionString("RoadieDatabaseConnection")
            ));

            services.AddDbContextPool<IRoadieDbContext, RoadieDbContext>(
                options => options.UseMySql(this._configuration.GetConnectionString("RoadieDatabaseConnection")
            ));

            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationUserDbContext>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
                options.AddPolicy("Editor", policy => policy.RequireRole("Editor"));
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
                return settings;
            });

            services.AddSingleton<IDefaultNotFoundImages, DefaultNotFoundImages>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<ICollectionService, CollectionService>();
            services.AddScoped<IPlaylistService, PlaylistService>();
            services.AddScoped<IArtistService, ArtistService>();
            services.AddScoped<IImageService, ImageService>();
            services.AddScoped<IReleaseService, ReleaseService>();
            services.AddScoped<ITrackService, TrackService>();
            services.AddScoped<ILabelService, LabelService>();
            services.AddScoped<IPlaylistService, PlaylistService>();
            services.AddScoped<IBookmarkService, BookmarkService>();
            services.AddScoped<IPlayActivityService, PlayActivityService>();
            services.AddScoped<IGenreService, GenreService>();
            services.AddScoped<ISubsonicService, SubsonicService>();
            services.AddScoped<IUserService, UserService>();

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

            services.AddOData();

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
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddHttpContextAccessor();
            services.AddScoped<IHttpContext>(factory =>
            {
                var actionContext = factory.GetService<IActionContextAccessor>()
                                           .ActionContext;
                return new HttpContext(new UrlHelper(actionContext));
            });
        }

        private static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<models.Artist>("Artist");
            builder.EntitySet<models.Label>("Label");
            builder.EntitySet<models.Releases.Release>("Release");
            return builder.GetEdmModel();
        }
    }
}