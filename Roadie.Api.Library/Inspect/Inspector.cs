using AutoCompare;
using HashidsNet;
using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.Inspect.Plugins.Directory;
using Roadie.Library.Inspect.Plugins.File;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.Processors;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;

namespace Roadie.Library.Inspect
{
    public class Inspector
    {
        private const string Salt = "6856F2EE-5965-4345-884B-2CCA457AAF59";

        private IRoadieSettings Configuration { get; }

        private ILogger Logger => MessageLogger as ILogger;

        private IEventMessageLogger MessageLogger { get; }

        private ID3TagsHelper TagsHelper { get; }

        private IEnumerable<IInspectorDirectoryPlugin> _directoryPlugins;

        private IEnumerable<IInspectorFilePlugin> _filePlugins;

        public DictionaryCacheManager CacheManager { get; }

        public IEnumerable<IInspectorDirectoryPlugin> DirectoryPlugins
        {
            get
            {
                if (_filePlugins == null)
                {
                    var plugins = new List<IInspectorDirectoryPlugin>();
                    try
                    {
                        var type = typeof(IInspectorDirectoryPlugin);
                        var types = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(s => s.GetTypes())
                            .Where(p => type.IsAssignableFrom(p));
                        foreach (var t in types)
                        {
                            if (t.GetInterface("IInspectorDirectoryPlugin") != null && !t.IsAbstract && !t.IsInterface)
                            {
                                var plugin = Activator.CreateInstance(t, Configuration, CacheManager, Logger, TagsHelper) as IInspectorDirectoryPlugin;
                                if (plugin.IsEnabled)
                                {
                                    plugins.Add(plugin);
                                }
                                else
                                {
                                    Console.WriteLine($"╠╣ Not Loading Disabled Plugin [{plugin.Description}]");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex);
                    }

                    _directoryPlugins = plugins.ToArray();
                }

                return _directoryPlugins;
            }
        }

        public IEnumerable<IInspectorFilePlugin> FilePlugins
        {
            get
            {
                if (_filePlugins == null)
                {
                    var plugins = new List<IInspectorFilePlugin>();
                    try
                    {
                        var type = typeof(IInspectorFilePlugin);
                        var types = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(s => s.GetTypes())
                            .Where(p => type.IsAssignableFrom(p));
                        foreach (var t in types)
                        {
                            if (t.GetInterface("IInspectorFilePlugin") != null && !t.IsAbstract && !t.IsInterface)
                            {
                                var plugin = Activator.CreateInstance(t, Configuration, CacheManager, Logger, TagsHelper) as IInspectorFilePlugin;
                                if (plugin.IsEnabled)
                                {
                                    plugins.Add(plugin);
                                }
                                else
                                {
                                    Console.WriteLine($"╠╣ Not Loading Disabled Plugin [{plugin.Description}]");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex);
                    }

                    _filePlugins = plugins.ToArray();
                }

                return _filePlugins;
            }
        }

        public Inspector(IHttpClientFactory httpClientFactory)
        {
            MessageLogger = new EventMessageLogger<Inspector>();
            MessageLogger.Messages += MessageLogger_Messages;

            var settings = new RoadieSettings();
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json", false);
            IConfiguration configuration = configurationBuilder.Build();
            configuration.GetSection("RoadieSettings").Bind(settings);
            settings.ConnectionString = configuration.GetConnectionString("RoadieDatabaseConnection");
            Configuration = settings;
            CacheManager = new DictionaryCacheManager(Logger, new NewtonsoftCacheSerializer(Logger), new CachePolicy(TimeSpan.FromHours(4)));

            var tagHelperLooper = new EventMessageLogger<ID3TagsHelper>();
            tagHelperLooper.Messages += MessageLogger_Messages;
            TagsHelper = new ID3TagsHelper(Configuration, CacheManager, tagHelperLooper, httpClientFactory);

        }

        private void InspectImage(bool isReadOnly, bool doCopy, string dest, string subdirectory, FileInfo image)
        {
            if (!image.Exists)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"╟ ■ InspectImage: Image Not Found [{image.FullName}]");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"╟─ Inspecting Image [{image.FullName}]");
            var newImageFolder = new DirectoryInfo(Path.Combine(dest, subdirectory));
            if (!newImageFolder.Exists)
            {
                newImageFolder.Create();
            }

            var newImagePath = Path.Combine(dest, subdirectory, image.Name);

            if (image.FullName != newImagePath)
            {
                var looper = 0;
                while (File.Exists(newImagePath))
                {
                    looper++;
                    newImagePath = Path.Combine(dest, subdirectory, looper.ToString("00"), image.Name);
                }
                if (isReadOnly)
                {
                    Console.WriteLine($"╟ 🔒 Read Only Mode: Would be [{(doCopy ? "Copied" : "Moved")}] to [{newImagePath}]");
                }
                else
                {
                    try
                    {
                        if (!doCopy)
                        {
                            image.MoveTo(newImagePath);
                        }
                        else
                        {
                            image.CopyTo(newImagePath, true);
                        }
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"╠═ 🚛 {(doCopy ? "Copied" : "Moved")} Image File to [{newImagePath}]");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"📛 Error file [{image.FullName}], newImagePath [{newImagePath}], Exception: [{ex}]");
                    }
                }
            }

            Console.ResetColor();
        }

        private void MessageLogger_Messages(object sender, EventMessage e)
        {
            Console.WriteLine($"Log Level [{e.Level}] Log Message [{e.Message}] ");
            var message = e.Message;
            switch (e.Level)
            {
                case LogLevel.Trace:
                    Logger.LogTrace(message);
                    break;

                case LogLevel.Debug:
                    Logger.LogDebug(message);
                    break;

                case LogLevel.Information:
                    Logger.LogInformation(message);
                    break;

                case LogLevel.Warning:
                    Logger.LogWarning(message);
                    break;

                case LogLevel.Critical:
                    Logger.LogCritical(message);
                    break;
            }
        }

        private string RunScript(string scriptFilename, bool doCopy, bool isReadOnly, string directoryToInspect, string dest)
        {
            if (string.IsNullOrEmpty(scriptFilename))
            {
                return null;
            }

            try
            {
                if (!File.Exists(scriptFilename))
                {
                    Console.WriteLine($"Script Not Found: [{scriptFilename}]");
                    return null;
                }

                Console.WriteLine($"Running Script: [{scriptFilename}]");
                var script = File.ReadAllText(scriptFilename);
                using (var ps = PowerShell.Create())
                {
                    var r = string.Empty;
                    var results = ps.AddScript(script)
                                    .AddParameter("DoCopy", doCopy)
                                    .AddParameter("IsReadOnly", isReadOnly)
                                    .AddParameter("DirectoryToInspect", directoryToInspect)
                                    .AddParameter("Dest", dest)
                                    .Invoke();
                    foreach (var result in results)
                    {
                        r += result + Environment.NewLine;
                    }
                    return r;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"📛 Error with Script File [{scriptFilename}], Error [{ex}] ");
            }
            return null;
        }

        public static string ArtistInspectorToken(AudioMetaData metaData) => ToToken(metaData.Artist);

        void PrintInspectorBanner(string directory)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("");
            Console.WriteLine(" ▄▄▄         ▄▄▄· ·▄▄▄▄  ▪  ▄▄▄ .    • ▌ ▄ ·. ▄▄▄ .·▄▄▄▄  ▪   ▄▄▄·     ▪   ▐ ▄ .▄▄ ·  ▄▄▄·▄▄▄ . ▄▄· ▄▄▄▄▄      ▄▄▄  ");
            Console.WriteLine(" ▀▄ █·▪     ▐█ ▀█ ██▪ ██ ██ ▀▄.▀·    ·██ ▐███▪▀▄.▀·██▪ ██ ██ ▐█ ▀█     ██ •█▌▐█▐█ ▀. ▐█ ▄█▀▄.▀·▐█ ▌▪•██  ▪     ▀▄ █·");
            Console.WriteLine(" ▐▀▀▄  ▄█▀▄ ▄█▀▀█ ▐█· ▐█▌▐█·▐▀▀▪▄    ▐█ ▌▐▌▐█·▐▀▀▪▄▐█· ▐█▌▐█·▄█▀▀█     ▐█·▐█▐▐▌▄▀▀▀█▄ ██▀·▐▀▀▪▄██ ▄▄ ▐█.▪ ▄█▀▄ ▐▀▀▄ ");
            Console.WriteLine(" ▐█•█▌▐█▌.▐▌▐█ ▪▐▌██. ██ ▐█▌▐█▄▄▌    ██ ██▌▐█▌▐█▄▄▌██. ██ ▐█▌▐█ ▪▐▌    ▐█▌██▐█▌▐█▄▪▐█▐█▪·•▐█▄▄▌▐███▌ ▐█▌·▐█▌.▐▌▐█•█▌");
            Console.WriteLine(" .▀  ▀ ▀█▄▀▪ ▀  ▀ ▀▀▀▀▀• ▀▀▀ ▀▀▀     ▀▀  █▪▀▀▀ ▀▀▀ ▀▀▀▀▀• ▀▀▀ ▀  ▀     ▀▀▀▀▀ █▪ ▀▀▀▀ .▀    ▀▀▀ ·▀▀▀  ▀▀▀  ▀█▄▀▪.▀  ▀");
            Console.WriteLine("");
            Console.ResetColor();

            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"✨ Inspector Start, UTC [{DateTime.UtcNow.ToString("s")}]");
            Console.ResetColor();

            if (!Directory.Exists(directory))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"📛 Folder [{directory}] is not found.");
                Console.ResetColor();
                return;
            }
        }

        public void GenerateRoadieDataFiles(string directory)
        {
            PrintInspectorBanner(directory);

            var roadieDataFilePlugin = DirectoryPlugins.FirstOrDefault(x => x.Description == RoadieDataFileCreator.RoadieDataFileCreatorDescription);
            if(roadieDataFilePlugin == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"📛 Unable to find Roadie Data File Creator Plugin.");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"╠╬═ Running Directory Plugin: {roadieDataFilePlugin.Description}");

            // Get all the top level directorys in the directory
            var directoryDirectories = Directory.GetDirectories(directory, "*.*", SearchOption.TopDirectoryOnly);
            foreach(var directoryDirectory in directoryDirectories)
            {
                var directoryInfo = new DirectoryInfo(directoryDirectory);

                var pluginResult = roadieDataFilePlugin.Process(directoryInfo);
                if (!pluginResult.IsSuccess)
                {
                    Console.WriteLine($"📛 Plugin Failed: Error [{CacheManager.CacheSerializer.Serialize(pluginResult)}]");
                    return;
                }
                if (!string.IsNullOrEmpty(pluginResult.Data))
                {
                    Console.WriteLine($"╠╣ Directory Plugin Message: {pluginResult.Data}");
                }
            }
        }

        public void Inspect(bool doCopy, bool isReadOnly, string directoryToInspect, string destination, bool dontAppendSubFolder, bool dontDeleteEmptyFolders, bool dontRunPreScripts)
        {
            Configuration.Inspector.IsInReadOnlyMode = isReadOnly;
            Configuration.Inspector.DoCopyFiles = doCopy;

            var artistsFound = new List<string>();
            var releasesFound = new List<string>();
            var mp3FilesFoundCount = 0;

            Trace.Listeners.Add(new LoggingTraceListener());

            PrintInspectorBanner(directoryToInspect);

            string scriptResult = null;
            // Run PreInspect script
            dontRunPreScripts = File.Exists(Configuration.Processing.PreInspectScript) && dontRunPreScripts;
            if (dontRunPreScripts)
            {
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Skipping PreInspectScript.");
                Console.ResetColor();
            }
            else
            {
                scriptResult = RunScript(Configuration.Processing.PreInspectScript, doCopy, isReadOnly, directoryToInspect, destination);
                if (!string.IsNullOrEmpty(scriptResult))
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"PreInspectScript Results: {Environment.NewLine + scriptResult + Environment.NewLine}");
                    Console.ResetColor();
                }
            }
            // Create a new destination subfolder for each Inspector run by Current timestamp
            var dest = Path.Combine(destination, DateTime.UtcNow.ToString("yyyyMMddHHmm"));
            if (isReadOnly || dontAppendSubFolder)
            {
                dest = destination;
            }
            // Get all the directorys in the directory
            var directoryDirectories = Directory.GetDirectories(directoryToInspect, "*.*", SearchOption.AllDirectories);
            var directories = new List<string>
            {
                directoryToInspect
            };
            directories.AddRange(directoryDirectories);
            directories.Remove(dest);
            var inspectedImagesInDirectories = new List<string>();
            try
            {
                var createdDestinationFolder = false;
                var sw = Stopwatch.StartNew();

                foreach (var directory in directories.OrderBy(x => x))
                {
                    var directoryInfo = new DirectoryInfo(directory);

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"╔ 📂 Inspecting [{directory}]");
                    Console.ResetColor();
                    Console.WriteLine("╠╦════════════════════════╣");

                    // Get all the MP3 files in 'directory'
                    var files = Directory.GetFiles(directory, "*.mp3", SearchOption.TopDirectoryOnly);
                    if (files?.Any() == true)
                    {
                        if (!isReadOnly && !createdDestinationFolder && !Directory.Exists(dest))
                        {
                            Directory.CreateDirectory(dest);
                            createdDestinationFolder = true;
                        }

                        // Run directory plugins against current directory
                        foreach (var plugin in DirectoryPlugins.Where(x => !x.IsPostProcessingPlugin).OrderBy(x => x.Order))
                        {
                            Console.WriteLine($"╠╬═ Running Directory Plugin: {plugin.Description}");
                            var pluginResult = plugin.Process(directoryInfo);
                            if (!pluginResult.IsSuccess)
                            {
                                Console.WriteLine($"📛 Plugin Failed: Error [{CacheManager.CacheSerializer.Serialize(pluginResult)}]");
                                return;
                            }
                            if (!string.IsNullOrEmpty(pluginResult.Data))
                            {
                                Console.WriteLine($"╠╣ Directory Plugin Message: {pluginResult.Data}");
                            }
                        }

                        Console.WriteLine("╠╝");
                        Console.WriteLine($"╟─ Found [{files.Length}] mp3 Files");
                        var fileMetaDatas = new List<AudioMetaData>();
                        var fileInfos = new List<FileInfo>();
                        // Inspect the found MP3 files in 'directory'
                        foreach (var file in files)
                        {
                            mp3FilesFoundCount++;
                            var fileInfo = new FileInfo(file);
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine($"╟─ 🎵 Inspecting [{fileInfo.FullName}]");
                            var tagLib = TagsHelper.MetaDataForFile(fileInfo.FullName, true);
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            if (!tagLib?.IsSuccess ?? false)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                            }

                            Console.WriteLine($"╟ (Pre ) : {tagLib.Data}");
                            Console.ResetColor();
                            tagLib.Data.Filename = fileInfo.FullName;
                            var originalMetaData = tagLib.Data.Adapt<AudioMetaData>();
                            if (!originalMetaData.IsValid)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine($"╟ ❗ INVALID: Missing: {ID3TagsHelper.DetermineMissingRequiredMetaData(originalMetaData)}");
                                Console.WriteLine($"╟ [{CacheManager.CacheSerializer.Serialize(tagLib)}]");
                                Console.ResetColor();
                            }

                            var pluginMetaData = tagLib.Data;
                            // Run all file plugins against the MP3 file modifying the MetaData
                            foreach (var plugin in FilePlugins.OrderBy(x => x.Order))
                            {
                                Console.WriteLine($"╟┤ Running File Plugin {plugin.Description}");
                                OperationResult<AudioMetaData> pluginResult = plugin.Process(pluginMetaData);
                                if (!pluginResult.IsSuccess)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"📛 Plugin Failed: Error [{CacheManager.CacheSerializer.Serialize(pluginResult)}]");
                                    Console.ResetColor();
                                    return;
                                }

                                pluginMetaData = pluginResult.Data;
                            }

                            if (!pluginMetaData.IsValid)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"╟ ❗ INVALID: Missing: {ID3TagsHelper.DetermineMissingRequiredMetaData(pluginMetaData)}");
                                Console.ResetColor();
                                return;
                            }

                            // See if the MetaData from the Plugins is different from the original
                            if (originalMetaData != null && pluginMetaData != null)
                            {
                                var differences = Comparer.Compare(originalMetaData, pluginMetaData);
                                if (differences.Count > 0)
                                {
                                    var skipDifferences = new List<string> { "AudioMetaDataWeights", "FileInfo", "Images", "TrackArtists" };
                                    var differencesDescription = $"{Environment.NewLine}";
                                    foreach (var difference in differences)
                                    {
                                        if (skipDifferences.Contains(difference.Name))
                                        {
                                            continue;
                                        }
                                        differencesDescription += $"╟ || {difference.Name} : Was [{difference.OldValue}] Now [{difference.NewValue}]{Environment.NewLine}";
                                    }

                                    Console.Write($"╟ ≡ != ID3 Tag Modified: {differencesDescription}");

                                    if (!isReadOnly)
                                    {
                                        if (!TagsHelper.WriteTags(pluginMetaData, pluginMetaData.Filename))
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("📛 WriteTags Failed");
                                            Console.ResetColor();
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("╟ 🔒 Read Only Mode: Not Modifying File ID3 Tags.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("╟ ≡ == ID3 Tag NOT Modified");
                                }
                            }
                            else
                            {
                                var oBad = originalMetaData == null;
                                var pBad = pluginMetaData == null;
                                Console.WriteLine($"╟ !! MetaData comparison skipped. {(oBad ? "Pre MetaData is Invalid" : string.Empty)} {(pBad ? "Post MetaData is Invalid" : string.Empty)}");
                            }

                            if (!pluginMetaData.IsValid)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"╟ ❗ INVALID: Missing: {ID3TagsHelper.DetermineMissingRequiredMetaData(pluginMetaData)}");
                                Console.ResetColor();
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"╟ (Post) : {pluginMetaData}");
                                Console.ResetColor();

                                var artistToken = ArtistInspectorToken(tagLib.Data);
                                if (!artistsFound.Contains(artistToken))
                                {
                                    artistsFound.Add(artistToken);
                                }
                                var releaseToken = ReleaseInspectorToken(tagLib.Data);
                                if (!releasesFound.Contains(releaseToken))
                                {
                                    releasesFound.Add(releaseToken);
                                }
                                var newFileName = $"CD{(tagLib.Data.Disc ?? ID3TagsHelper.DetermineDiscNumber(tagLib.Data)).ToString("000")}_{tagLib.Data.TrackNumber.Value.ToString("0000")}.mp3";
                                // Artist sub folder is created to hold Releases for Artist and Artist Images
                                var artistSubDirectory = directory == dest
                                    ? fileInfo.DirectoryName
                                    : Path.Combine(dest, artistToken);
                                // Each release is put into a subfolder into the current run Inspector folder to hold MP3 Files and Release Images
                                var subDirectory = directory == dest
                                    ? fileInfo.DirectoryName
                                    : Path.Combine(dest, artistToken, releaseToken);
                                if (!isReadOnly && !Directory.Exists(subDirectory))
                                {
                                    Directory.CreateDirectory(subDirectory);
                                }
                                // Inspect images
                                if (!inspectedImagesInDirectories.Contains(directoryInfo.FullName))
                                {
                                    // Get all artist images and move to artist folder
                                    var foundArtistImages = new List<FileInfo>();
                                    foundArtistImages.AddRange(ImageHelper.FindImagesByName(directoryInfo, tagLib.Data.Artist, SearchOption.TopDirectoryOnly));
                                    foundArtistImages.AddRange(ImageHelper.FindImagesByName(directoryInfo.Parent, tagLib.Data.Artist, SearchOption.TopDirectoryOnly));
                                    foundArtistImages.AddRange(ImageHelper.FindImageTypeInDirectory(directoryInfo.Parent, ImageType.Artist, SearchOption.TopDirectoryOnly));
                                    foundArtistImages.AddRange(ImageHelper.FindImageTypeInDirectory(directoryInfo.Parent, ImageType.ArtistSecondary, SearchOption.TopDirectoryOnly));
                                    foundArtistImages.AddRange(ImageHelper.FindImageTypeInDirectory(directoryInfo, ImageType.Artist, SearchOption.TopDirectoryOnly));
                                    foundArtistImages.AddRange(ImageHelper.FindImageTypeInDirectory(directoryInfo, ImageType.ArtistSecondary, SearchOption.TopDirectoryOnly));

                                    foreach (var artistImage in foundArtistImages)
                                    {
                                        InspectImage(isReadOnly, doCopy, dest, artistSubDirectory, artistImage);
                                    }

                                    // Get all release images and move to release folder
                                    var foundReleaseImages = new List<FileInfo>();
                                    foundReleaseImages.AddRange(ImageHelper.FindImagesByName(directoryInfo, tagLib.Data.Release));
                                    foundReleaseImages.AddRange(ImageHelper.FindImageTypeInDirectory(directoryInfo, ImageType.Release));
                                    foundReleaseImages.AddRange(ImageHelper.FindImageTypeInDirectory(directoryInfo, ImageType.ReleaseSecondary));
                                    foreach (var foundReleaseImage in foundReleaseImages)
                                    {
                                        InspectImage(isReadOnly, doCopy, dest, subDirectory, foundReleaseImage);
                                    }
                                    inspectedImagesInDirectories.Add(directoryInfo.FullName);
                                }

                                // If enabled move MP3 to new folder
                                var newPath = Path.Combine(dest, subDirectory, newFileName.ToFileNameFriendly());
                                if (isReadOnly)
                                {
                                    Console.WriteLine($"╟ 🔒 Read Only Mode: File would be [{(doCopy ? "Copied" : "Moved")}] to [{newPath}]");
                                }
                                else
                                {
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

                                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                                    Console.WriteLine($"╠═ 🚛 {(doCopy ? "Copied" : "Moved")} MP3 File to [{newPath}]");
                                    Console.ResetColor();
                                }

                                Console.WriteLine("╠════════════════════════╣");
                            }
                        }
                    }
                }

                foreach (var directory in directories.OrderBy(x => x))
                {
                    var directoryInfo = new DirectoryInfo(directory);
                    Console.WriteLine($"╠╬═ Post-Processing Directory [{directoryInfo.FullName}] ");

                    // Run post-processing directory plugins against current directory
                    foreach (var plugin in DirectoryPlugins.Where(x => x.IsPostProcessingPlugin).OrderBy(x => x.Order))
                    {
                        Console.WriteLine($"╠╬═ Running Post-Processing Directory Plugin {plugin.Description}");
                        var pluginResult = plugin.Process(directoryInfo);
                        if (!pluginResult.IsSuccess)
                        {
                            Console.WriteLine($"📛 Plugin Failed: Error [{CacheManager.CacheSerializer.Serialize(pluginResult)}]");
                            return;
                        }

                        if (!string.IsNullOrEmpty(pluginResult.Data))
                        {
                            Console.WriteLine($"╠╣ Directory Plugin Message: {pluginResult.Data}");
                        }
                    }
                }

                Console.WriteLine("╠╝");
                sw.Stop();
                Console.WriteLine($"╚═ Elapsed Time {sw.ElapsedMilliseconds.ToString("0000000")}, Artists {artistsFound.Count}, Releases {releasesFound.Count}, MP3s {mp3FilesFoundCount} ═╝");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                Console.WriteLine($"📛 Exception: {ex}");
            }

            if (!dontDeleteEmptyFolders)
            {
                var delEmptyFolderIn = new DirectoryInfo(directoryToInspect);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"❌ Deleting Empty folders in [{delEmptyFolderIn.FullName}]");
                Console.ResetColor();
                FolderPathHelper.DeleteEmptyFolders(delEmptyFolderIn);
            }
            else
            {
                Console.WriteLine("🔒 Read Only Mode: Not deleting empty folders.");
            }

            // Run PreInspect script
            scriptResult = RunScript(Configuration.Processing.PostInspectScript, doCopy, isReadOnly, directoryToInspect, destination);
            if (!string.IsNullOrEmpty(scriptResult))
            {
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"PostInspectScript Results: {Environment.NewLine + scriptResult + Environment.NewLine}");
                Console.ResetColor();
            }
        }

        public static string ReleaseInspectorToken(AudioMetaData metaData) => ToToken(metaData.Artist + metaData.Release);

        public static string ToToken(string input)
        {
            var hashids = new Hashids(Salt);
            var numbers = 0;
            var bytes = System.Text.Encoding.ASCII.GetBytes(input);
            var looper = bytes.Length / 4;
            for (var i = 0; i < looper; i++)
            {
                numbers += BitConverter.ToInt32(bytes, i * 4);
            }
            if (numbers < 0)
            {
                numbers *= -1;
            }

            return hashids.Encode(numbers);
        }
    }

    public class LoggingTraceListener : TraceListener
    {
        public override void Write(string message)
        {
            Console.WriteLine($"╠╬═ {message}");
        }

        public override void WriteLine(string message)
        {
            Console.WriteLine($"╠╬═ {message}");
        }
    }
}
