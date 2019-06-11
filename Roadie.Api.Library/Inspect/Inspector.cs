using HashidsNet;
using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
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
using System.Threading.Tasks;

namespace Roadie.Library.Inspect
{
    public class Inspector
    {
        private static readonly string Salt = "6856F2EE-5965-4345-884B-2CCA457AAF59";

        private IEnumerable<IInspectorDirectoryPlugin> _directoryPlugins = null;
        private IEnumerable<IInspectorFilePlugin> _filePlugins = null;
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
                        foreach (Type t in types)
                        {
                            if (t.GetInterface("IInspectorDirectoryPlugin") != null && !t.IsAbstract && !t.IsInterface)
                            {
                                IInspectorDirectoryPlugin plugin = Activator.CreateInstance(t, new object[] { Configuration, CacheManager, Logger, TagsHelper }) as IInspectorDirectoryPlugin;
                                plugins.Add(plugin);
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
                        foreach (Type t in types)
                        {
                            if (t.GetInterface("IInspectorFilePlugin") != null && !t.IsAbstract && !t.IsInterface)
                            {
                                IInspectorFilePlugin plugin = Activator.CreateInstance(t, new object[] { Configuration, CacheManager, Logger, TagsHelper }) as IInspectorFilePlugin;
                                plugins.Add(plugin);
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

        private IRoadieSettings Configuration { get; }

        private ILogger Logger
        {
            get
            {
                return MessageLogger as ILogger;
            }
        }

        private IEventMessageLogger MessageLogger { get; }
        private ID3TagsHelper TagsHelper { get; }

        public Inspector()
        {
            Console.WriteLine("Roadie Media Inspector");

            MessageLogger = new EventMessageLogger();
            MessageLogger.Messages += MessageLogger_Messages;

            var settings = new RoadieSettings();
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json");
            IConfiguration configuration = configurationBuilder.Build();
            configuration.GetSection("RoadieSettings").Bind(settings);
            settings.ConnectionString = configuration.GetConnectionString("RoadieDatabaseConnection");
            Configuration = settings;
            CacheManager = new DictionaryCacheManager(Logger, new CachePolicy(TimeSpan.FromHours(4)));
            TagsHelper = new ID3TagsHelper(Configuration, CacheManager, Logger);
        }

        public static string ArtistInspectorToken(AudioMetaData metaData) => ToToken(metaData.Artist);

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
            var token = hashids.Encode(numbers);
            return token;
        }

        public void Inspect(bool doCopy, bool isReadOnly, string directoryToInspect, string destination, bool dontAppendSubFolder, bool dontDeleteEmptyFolders)
        {
            Configuration.Inspector.IsInReadOnlyMode = isReadOnly;
            Configuration.Inspector.DoCopyFiles = doCopy;

            var artistsFound = new List<string>();
            var releasesFound = new List<string>();
            var mp3FilesFoundCount = 0;
            // Create a new destination subfolder for each Inspector run by Current timestamp
            var dest = Path.Combine(destination, DateTime.UtcNow.ToString("yyyyMMddHHmm"));
            if (isReadOnly || dontAppendSubFolder)
            {
                dest = destination;
            }
            if (!isReadOnly && !Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
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
                foreach (var directory in directories.OrderBy(x => x))
                {
                    var directoryInfo = new DirectoryInfo(directory);

                    var sw = Stopwatch.StartNew();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"╔ ░▒▓ Inspecting [{ directory }] ▓▒░");
                    Console.ResetColor();
                    Console.WriteLine("╠╦════════════════════════╣");

                    // Get all the MP3 files in 'directory'
                    var files = Directory.GetFiles(directory, "*.mp3", SearchOption.TopDirectoryOnly);
                    if (files == null || !files.Any())
                    {
                        continue;
                    }
                    // Run directory plugins against current directory
                    foreach (var plugin in DirectoryPlugins.OrderBy(x => x.Order))
                    {
                        Console.WriteLine($"╠╬═ Running Directory Plugin { plugin.Description }");
                        var pluginResult = plugin.Process(directoryInfo);
                        if (!pluginResult.IsSuccess)
                        {
                            Console.WriteLine($"Plugin Failed: Error [{ JsonConvert.SerializeObject(pluginResult)}]");
                            return;
                        }
                        else if (!string.IsNullOrEmpty(pluginResult.Data))
                        {
                            Console.WriteLine($"╠╣ Directory Plugin Message: { pluginResult.Data }");
                        }
                    }
                    Console.WriteLine($"╠╝");
                    Console.WriteLine($"╟─ Found [{ files.Length }] mp3 Files");
                    List<AudioMetaData> fileMetaDatas = new List<AudioMetaData>();
                    List<FileInfo> fileInfos = new List<FileInfo>();
                    // Inspect the found MP3 files in 'directory'
                    foreach (var file in files)
                    {
                        mp3FilesFoundCount++;
                        var fileInfo = new FileInfo(file);
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine($"╟─ Inspecting [{ fileInfo.FullName }]");
                        var tagLib = TagsHelper.MetaDataForFile(fileInfo.FullName, true);
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        if (!tagLib?.IsSuccess ?? false)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                        }
                        Console.WriteLine($"╟ (Pre ) : { tagLib.Data }");
                        Console.ResetColor();
                        tagLib.Data.Filename = fileInfo.FullName;
                        var originalMetaData = tagLib.Data.Adapt<AudioMetaData>();
                        var pluginMetaData = tagLib.Data;
                        // Run all file plugins against the MP3 file modifying the MetaData
                        foreach (var plugin in FilePlugins.OrderBy(x => x.Order))
                        {
                            Console.WriteLine($"╟┤ Running File Plugin { plugin.Description }");
                            OperationResult<AudioMetaData> pluginResult = null;
                            pluginResult = plugin.Process(pluginMetaData);
                            if (!pluginResult.IsSuccess)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Plugin Failed: Error [{ JsonConvert.SerializeObject(pluginResult)}]");
                                Console.ResetColor();
                                return;
                            }
                            pluginMetaData = pluginResult.Data;
                        }
                        // See if the MetaData from the Plugins is different from the original
                        if (originalMetaData != null && pluginMetaData != null)
                        {
                            var differences = AutoCompare.Comparer.Compare(originalMetaData, pluginMetaData);
                            if (differences.Any())
                            {
                                var skipDifferences = new List<string> { "AudioMetaDataWeights", "FileInfo", "Images", "TrackArtists", "ValidWeight" };
                                var differencesDescription = $"{ System.Environment.NewLine }";
                                foreach (var difference in differences)
                                {
                                    if (skipDifferences.Contains(difference.Name))
                                    {
                                        continue;
                                    }
                                    differencesDescription += $"╟ || { difference.Name } : Was [{ difference.OldValue}] Now [{ difference.NewValue}]{ System.Environment.NewLine }";
                                }
                                Console.Write($"╟ ≡ != ID3 Tag Modified: { differencesDescription }");

                                if (!isReadOnly)
                                {
                                    TagsHelper.WriteTags(pluginMetaData, pluginMetaData.Filename);
                                }
                                else
                                {
                                    Console.WriteLine("╟ ■ Read Only Mode: Not Modifying File ID3 Tags.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"╟ ≡ == ID3 Tag NOT Modified");
                            }
                        }
                        else
                        {
                            var oBad = originalMetaData == null;
                            var pBad = pluginMetaData == null;
                            Console.WriteLine($"╟ !! MetaData comparison skipped. { (oBad ? "Pre MetaData is Invalid" : "")} { (pBad ? "Post MetaData is Invalid" : "") }");
                        }
                        if (!pluginMetaData.IsValid)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"╟ ■■ INVALID: Missing: { ID3TagsHelper.DetermineMissingRequiredMetaData(pluginMetaData) }");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"╟ (Post) : { pluginMetaData }");
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
                            var newFileName = $"CD{ (tagLib.Data.Disc ?? ID3TagsHelper.DetermineDiscNumber(tagLib.Data)).ToString("000") }_{ tagLib.Data.TrackNumber.Value.ToString("0000") }.mp3";
                            // Artist sub folder is created to hold Releases for Artist and Artist Images
                            var artistSubDirectory = directory == dest ? fileInfo.DirectoryName : Path.Combine(dest, artistToken);
                            // Each release is put into a subfolder into the current run Inspector folder to hold MP3 Files and Release Images
                            var subDirectory = directory == dest ? fileInfo.DirectoryName : Path.Combine(dest, artistToken, releaseToken);
                            if (!isReadOnly && !Directory.Exists(subDirectory))
                            {
                                Directory.CreateDirectory(subDirectory);
                            }
                            // If enabled move MP3 to new folder
                            var newPath = Path.Combine(dest, subDirectory, newFileName.ToFileNameFriendly());
                            if (isReadOnly)
                            {
                                Console.WriteLine($"╟ ■ Read Only Mode: File would be [{ (doCopy ? "Copied" : "Moved") }] to [{ newPath }]");
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
                                Console.WriteLine($"╠═» { (doCopy ? "Copied" : "Moved")} MP3 File to [{ newPath }]");
                                Console.ResetColor();
                            }
                            if (!inspectedImagesInDirectories.Contains(directoryInfo.FullName))
                            {
                                // Get all artist images and move to artist folder
                                var foundArtistImages = new List<FileInfo>();
                                foundArtistImages.AddRange(ImageHelper.FindImageTypeInDirectory(directoryInfo.Parent, Enums.ImageType.Artist, SearchOption.TopDirectoryOnly));
                                foundArtistImages.AddRange(ImageHelper.FindImageTypeInDirectory(directoryInfo.Parent, Enums.ImageType.ArtistSecondary, SearchOption.TopDirectoryOnly));
                                foundArtistImages.AddRange(ImageHelper.FindImageTypeInDirectory(directoryInfo, Enums.ImageType.Artist, SearchOption.TopDirectoryOnly));
                                foundArtistImages.AddRange(ImageHelper.FindImageTypeInDirectory(directoryInfo, Enums.ImageType.ArtistSecondary, SearchOption.TopDirectoryOnly));

                                foreach (var artistImage in foundArtistImages)
                                {
                                    InspectImage(isReadOnly, doCopy, dest, artistSubDirectory, artistImage);
                                }

                                // Get all release images and move to release folder
                                var foundReleaseImages = new List<FileInfo>();
                                foundReleaseImages.AddRange(ImageHelper.FindImageTypeInDirectory(directoryInfo, Enums.ImageType.Release, SearchOption.AllDirectories));
                                foundReleaseImages.AddRange(ImageHelper.FindImageTypeInDirectory(directoryInfo, Enums.ImageType.ReleaseSecondary, SearchOption.AllDirectories));
                                foreach (var foundReleaseImage in foundReleaseImages)
                                {
                                    InspectImage(isReadOnly, doCopy, dest, subDirectory, foundReleaseImage);
                                }
                                inspectedImagesInDirectories.Add(directoryInfo.FullName);
                            }
                            Console.WriteLine("╠════════════════════════╣");
                        }
                    }
                    sw.Stop();
                    Console.WriteLine($"╚═ Elapsed Time { sw.ElapsedMilliseconds.ToString("0000000") }, Artists { artistsFound.Count() }, Releases { releasesFound.Count() }, MP3s { mp3FilesFoundCount } ═╝");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                Console.WriteLine("!! Exception: " + ex.ToString());
            }
            if (!dontDeleteEmptyFolders)
            {
                var delEmptyFolderIn = new DirectoryInfo(directoryToInspect);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"X Deleting Empty folders in [{ delEmptyFolderIn.FullName }]");
                Console.ResetColor();
                FolderPathHelper.DeleteEmptyDirs(directoryToInspect, false);
            }
            else
            {
                Console.WriteLine("X ■ Read Only Mode: Not deleting empty folders.");
            }
        }

        private void InspectImage(bool isReadOnly, bool doCopy, string dest, string subdirectory, FileInfo image)
        {
            Console.WriteLine($"╟─ Inspecting Image [{ image.FullName }]");
            var newImagePath = Path.Combine(dest, subdirectory, image.Name);
            if (isReadOnly)
            {
                Console.WriteLine($"╟ ■ Read Only Mode: Would be [{ (doCopy ? "Copied" : "Moved") }] to [{ newImagePath }]");
            }
            else
            {
                if (!doCopy)
                {
                    if (image.FullName != newImagePath)
                    {
                        if (File.Exists(newImagePath))
                        {
                            File.Delete(newImagePath);
                        }
                        image.MoveTo(newImagePath);
                    }
                }
                else
                {
                    image.CopyTo(newImagePath, true);
                }
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"╠═» { (doCopy ? "Copied" : "Moved")} Image File to [{ newImagePath }]");
                Console.ResetColor();
            }
        }

        private void MessageLogger_Messages(object sender, EventMessage e) => Console.WriteLine($"Log Level [{ e.Level }] Log Message [{ e.Message }] ");
    }
}