using HashidsNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.Inspect.Plugins;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.Processors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Roadie.Library.Inspect
{
    public class Inspector
    {
        private static readonly string Salt = "6856F2EE-5965-4345-884B-2CCA457AAF59";

        private IEnumerable<IInspectorPlugin> _plugins = null;

        public IEnumerable<IInspectorPlugin> Plugins
        {
            get
            {
                if (this._plugins == null)
                {
                    var plugins = new List<IInspectorPlugin>();
                    try
                    {
                        var type = typeof(IInspectorPlugin);
                        var types = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(s => s.GetTypes())
                            .Where(p => type.IsAssignableFrom(p));
                        foreach (Type t in types)
                        {
                            if (t.GetInterface("IInspectorPlugin") != null && !t.IsAbstract && !t.IsInterface)
                            {
                                IInspectorPlugin plugin = Activator.CreateInstance(t, new object[] { this.Configuration, this.CacheManager, this.Logger }) as IInspectorPlugin;
                                plugins.Add(plugin);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Logger.LogError(ex);
                    }
                    this._plugins = plugins.ToArray();
                }
                return this._plugins;
            }
        }

        private IEventMessageLogger MessageLogger { get; }
        private ILogger Logger
        {
            get
            {
                return this.MessageLogger as ILogger;
            }
        }

        private ID3TagsHelper TagsHelper { get; }

        private IRoadieSettings Configuration { get; }

        public DictionaryCacheManager CacheManager { get; }


        public Inspector()
        {
            Console.WriteLine("Roadie Media Inspector");


            this.MessageLogger = new EventMessageLogger();
            this.MessageLogger.Messages += MessageLogger_Messages;

            var settings = new RoadieSettings();
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json");
            IConfiguration configuration = configurationBuilder.Build();
            configuration.GetSection("RoadieSettings").Bind(settings);
            settings.ConnectionString = configuration.GetConnectionString("RoadieDatabaseConnection");
            this.Configuration = settings;
            this.CacheManager = new DictionaryCacheManager(this.Logger, new CachePolicy(TimeSpan.FromHours(4)));
            this.TagsHelper = new ID3TagsHelper(this.Configuration, this.CacheManager, this.Logger);

        }

        private void MessageLogger_Messages(object sender, EventMessage e)
        {
            Console.WriteLine($"Log Level [{ e.Level }] Log Message [{ e.Message }] ");
        }

        public static string ArtistInspectorToken(AudioMetaData metaData)
        {
            var hashids = new Hashids(Inspector.Salt);
            var artistId = 0;
            var bytes = System.Text.Encoding.ASCII.GetBytes(metaData.Artist);
            var looper = bytes.Length / 4;
            for(var i = 0; i < looper; i++)
            {
                artistId += BitConverter.ToInt32(bytes, i * 4);
            }
            if (artistId < 0)
            {
                artistId = artistId * -1;
            }
            var token = hashids.Encode(artistId);
            return token;
        }

        public static string ReleaseInspectorToken(AudioMetaData metaData)
        {
            var hashids = new Hashids(Inspector.Salt);
            var releaseId = 0;
            var bytes = System.Text.Encoding.ASCII.GetBytes(metaData.Artist + metaData.Release);
            var looper = bytes.Length / 4;
            for (var i = 0; i < looper; i++)
            {
                releaseId += BitConverter.ToInt32(bytes, i * 4);
            }
            if (releaseId < 0)
            {
                releaseId = releaseId * -1;
            }
            var token = hashids.Encode(releaseId);
            return token;
        }

        public void Inspect(bool doCopy, string folder, string destination)
        {
            // Get all the directorys in the directory
            var folderDirectories = Directory.GetDirectories(folder, "*.*", SearchOption.AllDirectories);
            var directories = new List<string>
            {
                folder
            };
            directories.AddRange(folderDirectories);
            foreach (var directory in directories)
            {
                Console.WriteLine($"╔ ░▒▓ Inspecting [{ directory }] ▓▒░");
                Console.WriteLine("╠═╗");
                // Get all the MP3 files in the folder
                var files = Directory.GetFiles(directory, "*.mp3", SearchOption.TopDirectoryOnly);
                if (files == null || !files.Any())
                {
                    continue;
                }
                Console.WriteLine($"Found [{ files.Length }] mp3 Files");
                Console.WriteLine("╠═╣");
                // Get audiometadata and output details including weight/validity
                foreach (var file in files)
                {
                    var tagLib = this.TagsHelper.MetaDataForFile(file);
                    Console.WriteLine(tagLib.Data);
                }
                Console.WriteLine("╠═╣");
                List<AudioMetaData> fileMetaDatas = new List<AudioMetaData>();
                List<FileInfo> fileInfos = new List<FileInfo>();
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    var tagLib = this.TagsHelper.MetaDataForFile(fileInfo.FullName);
                    var artistToken = ArtistInspectorToken(tagLib.Data);
                    var releaseToken = ReleaseInspectorToken(tagLib.Data);
                    var newFileName = $"{artistToken}_{releaseToken}_CD{ (tagLib.Data.Disk ?? ID3TagsHelper.DetermineDiscNumber(tagLib.Data)).ToString("000") }_{ tagLib.Data.TrackNumber.Value.ToString("0000") }.mp3";
                    var subFolder = folder == destination ? fileInfo.DirectoryName : destination;
                    var newPath = Path.Combine(destination, subFolder, newFileName.ToFileNameFriendly());
                    if (!doCopy)
                    {
                        if (fileInfo.FullName != newPath)
                        {
                            if (File.Exists(newPath))
                            {
                                File.Delete(newPath);
                            }
                            fileInfo.MoveTo(newPath);
                        }
                    }
                    else
                    {
                        fileInfo.CopyTo(newPath, true);
                    }
                    tagLib.Data.Filename = fileInfo.FullName;
                    fileMetaDatas.Add(tagLib.Data);
                    fileInfos.Add(fileInfo);
                }
                // Perform InspectorPlugins
                IEnumerable<AudioMetaData> pluginMetaData = fileMetaDatas.OrderBy(x => x.Filename);
                foreach (var plugin in this.Plugins.OrderBy(x => x.Order))
                {
                    Console.WriteLine($"╟ Running Plugin { plugin.Description }");
                    OperationResult<IEnumerable<AudioMetaData>> pluginResult = null;
                    try
                    {
                        pluginResult = plugin.Process(pluginMetaData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Plugin Error: [{ ex.ToString() }]");
                    }
                    if (!pluginResult.IsSuccess)
                    {
                        Console.WriteLine($"Plugin Failed. Error [{ JsonConvert.SerializeObject(pluginResult)}]");
                    }
                    pluginMetaData = pluginResult.Data;
                }
                // Save all plugin modifications to the MetaData
                foreach (var metadata in pluginMetaData)
                {
                    this.TagsHelper.WriteTags(metadata, metadata.Filename);
                }
                Console.WriteLine("╠═╣");
                // Get audiometadata and output details including weight/validity
                foreach (var fileInfo in fileInfos)
                {
                    var tagLib = this.TagsHelper.MetaDataForFile(fileInfo.FullName);
                    if (!tagLib.IsSuccess || !tagLib.Data.IsValid)
                    {
                        Console.WriteLine($"■■ INVALID: {tagLib.Data }");
                    }
                    else
                    {
                        Console.WriteLine(tagLib.Data);
                    }
                }
                Console.WriteLine("╚═╝");
            }
        }


    }
}
