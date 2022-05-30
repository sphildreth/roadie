using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.ID3Tags;
using System;

namespace Roadie.Library.Inspect.Plugins.File
{
    public class CleanUpArtists : FilePluginBase
    {
        public override string Description => "Clean: Artist (TPE1) and TrackArtist (TOPE)";

        public override int Order => 5;

        public CleanUpArtists(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger, IID3TagsHelper tagsHelper)
            : base(configuration, cacheManager, logger, tagsHelper)
        {
        }

        public string CleanArtist(string artist, string trackArtist = null)
        {
            artist ??= string.Empty;
            trackArtist ??= string.Empty;
            var splitCharacter = AudioMetaData.ArtistSplitCharacter.ToString();

            // Replace seperators with proper split character
            foreach (var replace in ListReplacements)
            {
                artist = artist.Replace(replace, splitCharacter, StringComparison.OrdinalIgnoreCase);
            }
            var originalArtist = artist;
            var result = artist.CleanString(Configuration, Configuration.Processing.ArtistRemoveStringsRegex).ToTitleCase(false);
            if (string.IsNullOrEmpty(result))
            {
                result = originalArtist;
            }
            if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(trackArtist))
            {
                result = result.Replace(splitCharacter + trackArtist + splitCharacter, string.Empty, StringComparison.OrdinalIgnoreCase);
                result = result.Replace(trackArtist + splitCharacter, string.Empty, StringComparison.OrdinalIgnoreCase);
                result = result.Replace(splitCharacter + trackArtist, string.Empty, StringComparison.OrdinalIgnoreCase);
                result = result.Replace(trackArtist, string.Empty, StringComparison.OrdinalIgnoreCase);
            }
            if (Configuration.Processing.DoDetectFeatureFragments)
            {
                if (!string.IsNullOrWhiteSpace(result))
                {
                    if (result.HasFeaturingFragments())
                    {
                        throw new RoadieProcessingException($"Artist name [{ result }] has Feature fragments.");
                    }
                }
            }
            return string.IsNullOrEmpty(result) ? null : result;
        }

        public override OperationResult<AudioMetaData> Process(AudioMetaData metaData)
        {
            var result = new OperationResult<AudioMetaData>();
            if (Configuration.Processing.DoAudioCleanup)
            {
                metaData.Artist = CleanArtist(metaData.ArtistRaw);
                metaData.TrackArtist = CleanArtist(metaData.TrackArtistRaw, metaData.ArtistRaw);
            }

            result.Data = metaData;
            result.IsSuccess = true;
            return result;
        }
    }
}
