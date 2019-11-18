using Roadie.Library.Extensions;
using System;
using System.Diagnostics;
using System.IO;

namespace Roadie.Library.Utility
{
    public static class FileMetaDataHelper
    {
        public static short? ReadNumberOfTrackFromCue(string cueFilename)
        {
            if (!File.Exists(cueFilename))
            {
                return null;
            }
            short? results = 0;
            try
            {
                using (var reader = new StreamReader(cueFilename))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            if (line.StartsWith("FILE", StringComparison.OrdinalIgnoreCase))
                            {
                                results++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error Reading Cue [{ cueFilename }] [{ ex }]");
            }
            return results;
        }

        public static short? ReadNumberOfTracksFromSfv(string sfvFilename)
        {
            if (!File.Exists(sfvFilename))
            {
                return null;
            }
            short? results = 0;
            try
            {
                using (var reader = new StreamReader(sfvFilename))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            if (!line.StartsWith(";"))
                            {
                                if (line.Contains(".mp3", StringComparison.OrdinalIgnoreCase) ||
                                   line.Contains(".flac", StringComparison.OrdinalIgnoreCase) ||
                                   line.Contains(".wave", StringComparison.OrdinalIgnoreCase) ||
                                   line.Contains(".mp4", StringComparison.OrdinalIgnoreCase))
                                {
                                    results++;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error Reading Sfv [{ sfvFilename }] [{ ex }]");
            }
            return results;
        }

        public static short? ReadNumberOfTrackFromM3u(string m3uFilename)
        {
            if (!File.Exists(m3uFilename))
            {
                return null;
            }
            short? results = 0;
            try
            {
                using (var reader = new StreamReader(m3uFilename))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            if (!line.StartsWith("#"))
                            {
                                results++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error Reading Cue [{ m3uFilename }] [{ ex }]");
            }
            return results;
        }

        public static bool FixCueFlacReferences(string cueFilename, bool writeUpdate = true)
        {
            if (!File.Exists(cueFilename))
            {
                return false;
            }
            var cueFile = new FileInfo(cueFilename);
            var cueTempFileName = Path.ChangeExtension(cueFilename, ".cue_tmp");
            using (var reader = new StreamReader(cueFilename))
            {
                using (var writer = new StreamWriter(cueTempFileName))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            if (line.StartsWith("File", StringComparison.OrdinalIgnoreCase))
                            {
                                line = line.Replace(".flac", ".mp3", StringComparison.OrdinalIgnoreCase)
                                           .Replace(".wav", ".mp3", StringComparison.OrdinalIgnoreCase)
                                           .ReplaceLastOccurrence(" WAVE", " MP3");
                            }
                        }
                        writer.WriteLine(line);
                    }
                }
                if (writeUpdate)
                {
                    cueFile.Delete();
                    File.Move(cueTempFileName, cueFilename);
                }
            }
            return true;
        }
    }
}