using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Dlna.Server;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Extensions;
using Roadie.Library.Models;
using Roadie.Library.Models.Releases;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Dlna.Services
{
    public class DlnaService : IMediaServer
    {
        private Dictionary<string, DateTimeOffset> LastTimePlayedForToken = new Dictionary<string, DateTimeOffset>();
        private object lockObject = new object();
        public IHttpAuthorizationMethod Authorizer { get; set; }
        public string FriendlyName { get; }
        public Guid UUID { get; } = Guid.NewGuid();
        private ICacheManager CacheManager { get; }
        private IRoadieSettings Configuration { get; }
        private IRoadieDbContext DbContext { get; }
        private IImageService ImageService { get; }
        private ILogger Logger { get; }
        private IPlayActivityService PlayActivityService { get; }
        private int RandomTrackLimit { get; }
        private ITrackService TrackService { get; }

        public DlnaService(IRoadieSettings configuration, IRoadieDbContext dbContext, ICacheManager cacheManager,
                           ILogger logger, IImageService imageService, ITrackService trackService, IPlayActivityService playActivityService)
        {
            Configuration = configuration;
            DbContext = dbContext;
            CacheManager = cacheManager;
            Logger = logger;
            FriendlyName = configuration.Dlna.FriendlyName;
            ImageService = imageService;
            TrackService = trackService;
            PlayActivityService = playActivityService;
            RandomTrackLimit = 50;
        }

        public void Preload()
        {
            var sw = Stopwatch.StartNew();
            RootFolder();
            sw.Stop();
            Logger.LogInformation($"DLNA Service Preload Complete. Elapsed Time [{ sw.Elapsed }]");
        }

        public IMediaItem GetItem(string id, bool isFileRequest)
        {
            if (id.Equals(Identifiers.GENERAL_ROOT))
            {
                return RootFolder();
            }
            if (id.Equals("vf:artists"))
            {
                return Artists();
            }
            if (id.Equals("vf:collections"))
            {
                return Collections();
            }
            if (id.Equals("vf:playlists"))
            {
                return Playlists();
            }
            if (id.Equals("vf:releases"))
            {
                return Releases();
            }
            if (id.Equals("vf:randomizer"))
            {
                return Randomizer();
            }
            if (id.Equals("vf:randomtracks"))
            {
                return RandomOrRatedTracks(false);
            }
            if (id.Equals("vf:randomratedtracks"))
            {
                return RandomOrRatedTracks(true);
            }
            if (id.StartsWith("vf:tracksforplaylist:"))
            {
                return TracksForPlaylist(id);
            }
            if (id.StartsWith("vf:artistsforfolder:"))
            {
                return ArtistsForFolder(id);
            }
            if (id.StartsWith("vf:releasesforcollection"))
            {
                return ReleasesForCollectionFolder(id);
            }
            if (id.StartsWith("vf:releasesforfolder:"))
            {
                return ReleasesForFolder(id);
            }
            if (id.StartsWith("vf:artist:"))
            {
                return ReleasesForArtist(id);
            }
            if (id.StartsWith("vf:release:"))
            {
                return TracksForRelease(id);
            }
            if (id.StartsWith("r:t:"))
            {
                return TrackDetail(id, isFileRequest);
            }
            Logger.LogWarning($"Unknown Item Key [{ id }]");

            throw new NotImplementedException();
        }

        private IEnumerable<string> ArtistGroupKeys()
        {
            lock (lockObject)
            {
                return CacheManager.Get("urn:DlnaService:Artists", () =>
                {
                    IEnumerable<string> result = new string[0];
                    try
                    {
                        var sw = Stopwatch.StartNew();
                        result = (from a in DbContext.Artists
                                  where a.ReleaseCount > 0
                                  select a)
                                  .ToArray()
                                  .Select(x => x.GroupBy)
                                  .Distinct();                                  

                        sw.Stop();
                        Logger.LogDebug($"DLNA ArtistGroupKeys fetch Elapsed Time [{ sw.Elapsed }]");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex);
                    }
                    return result;
                }, "urn:DlnaServiceRegion");
            }
        }

        /// <summary>
        /// Returns groups of artists for level 2
        /// </summary>
        /// <returns></returns>
        private IMediaFolder Artists()
        {
            try
            {
                var result = new VirtualFolder()
                {
                    Name = "Artists",
                    Id = "vf:artists"
                };
                foreach (var groupKey in ArtistGroupKeys())
                {
                    var f = new VirtualFolder(result, groupKey, $"vf:artistsforfolder:{ groupKey }");
                    foreach (var artistForGroup in ArtistsForGroup(groupKey))
                    {
                        var af = new VirtualFolder(f, artistForGroup.RoadieId.ToString(), $"vf:artist:{ artistForGroup.Id }");
                        f.AddFolder(af);
                    }
                    result.AddFolder(f);
                }
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Artists Root");
            }
            return null;
        }

        /// <summary>
        /// Returns artists for group letter (level 3)
        /// </summary>
        private IMediaItem ArtistsForFolder(string id)
        {
            var artistsForFolderKey = id.Replace("vf:artistsforfolder:", "");
            var result = new VirtualFolder()
            {
                Name = artistsForFolderKey,
                Id = id
            };

            foreach (var artistForGroup in ArtistsForGroup(artistsForFolderKey))
            {
                var af = new VirtualFolder(result, artistForGroup.SortName ?? artistForGroup.Name, $"vf:artist:{ artistForGroup.Id }");
                foreach (var artistRelease in ReleasesForArtist(artistForGroup.Id))
                {
                    var fr = new VirtualFolder(af, artistRelease.RoadieId.ToString(), $"vf:release:{ artistRelease.Id }");
                    af.AddFolder(fr);
                }
                result.AddFolder(af);
            }
            return result;
        }

        private IEnumerable<data.Artist> ArtistsForGroup(string groupKey)
        {
            lock (lockObject)
            {
                return CacheManager.Get($"urn:DlnaService:ArtistsForGroup:{ groupKey }", () =>
                {
                    return DbContext.Artists.AsEnumerable().Where(x => x.GroupBy == groupKey).Distinct().ToArray();
                }, "urn:DlnaServiceRegion");
            }
        }

        private IEnumerable<data.Collection> CollectionGroups()
        {
            lock (lockObject)
            {
                return CacheManager.Get("urn:DlnaService:Collections", () =>
                {
                    return (from c in DbContext.Collections
                            let sn = (c.SortName ?? c.Name).ToUpper()
                            orderby sn
                            select c).ToArray();
                }, "urn:DlnaServiceRegion");
            }
        }

        private IMediaFolder Collections()
        {
            var result = new VirtualFolder()
            {
                Name = "Collections",
                Id = "vf:collections"
            };
            foreach (var cg in CollectionGroups())
            {
                var f = new VirtualFolder(result, cg.SortName ?? cg.Name, $"vf:releasesforcollection:{ cg.Id }");
                foreach (var releaseForCollection in ReleasesForCollection(cg.Id))
                {
                    var af = new VirtualFolder(f, releaseForCollection.RoadieId.ToString(), $"vf:release:{ releaseForCollection.Id }");
                    f.AddFolder(af);
                }
                result.AddFolder(f);
            }
            return result;
        }

        //private IEnumerable<data.Collection> CollectionsForGroup(string groupKey)
        //{
        //    lock (lockObject)
        //    {
        //        return CacheManager.Get($"urn:DlnaService:CollectionsForGroup:{ groupKey }", () =>
        //        {
        //            return (from c in DbContext.Collections
        //                    let sn = (c.SortName ?? c.Name).ToUpper()
        //                    where sn == groupKey
        //                    select c).Distinct().ToArray();
        //        }, "urn:DlnaServiceRegion");
        //    }
        //}

        private IEnumerable<data.Playlist> PlaylistGroups()
        {
            lock (lockObject)
            {
                return CacheManager.Get("urn:DlnaService:Playlists", () =>
                {
                    return (from p in DbContext.Playlists
                            orderby p.Name
                            select p).ToArray();
                }, "urn:DlnaServiceRegion");
            }
        }

        private IMediaFolder Playlists()
        {
            var result = new VirtualFolder()
            {
                Name = "Playlists",
                Id = "vf:playlists"
            };
            foreach (var pl in PlaylistGroups())
            {
                var f = new VirtualFolder(result, pl.Name, $"vf:tracksforplaylist:{ pl.Id }");
                foreach (var track in TracksForPlaylist(pl.Id))
                {
                    var t = new VirtualFolder(result, pl.Name, $"t:tk:{track.Id}::{Guid.NewGuid()}");
                    f.AddFolder(t);
                }
                result.AddFolder(f);
            }

            return result;
        }

        private IMediaFolder Randomizer()
        {
            var result = new VirtualFolder()
            {
                Name = "Randomizer",
                Id = "vf:randomizer"
            };
            var randomTracks = new VirtualFolder()
            {
                Name = "Random Tracks",
                Id = "vf:randomtracks"
            };
            for (var i = 0; i < RandomTrackLimit; i++)
            {
                randomTracks.AddFolder(new VirtualFolder());
            }
            result.AddFolder(randomTracks);
            var randomRatedTracks = new VirtualFolder()
            {
                Name = "Random Rated Tracks",
                Id = "vf:randomratedtracks"
            };
            for (var i = 0; i < RandomTrackLimit; i++)
            {
                randomRatedTracks.AddFolder(new VirtualFolder());
            }
            result.AddFolder(randomRatedTracks);
            return result;
        }

        private IMediaFolder RandomOrRatedTracks(bool isRated)
        {
            var result = new VirtualFolder()
            {
                Name = isRated ? "Random Rated Tracks" : "Random Tracks",
                Id = isRated ? "vf:randomratedtracks" : "vf:randomtracks"
            };

            foreach (var randomTrack in RandomTracks(RandomTrackLimit, (short)(isRated ? 1 : 0)))
            {
                var t = new Track($"r:t:tk:{randomTrack.ReleaseMedia.Release.Id}:{randomTrack.Id}:{ Guid.NewGuid() }", randomTrack.ReleaseMedia.Release.Artist.Name, randomTrack.ReleaseMedia.Release.Title, randomTrack.ReleaseMedia.MediaNumber,
                                    randomTrack.Title, randomTrack.ReleaseMedia.Release.Genres.Select(x => x.Genre.Name).ToCSV(), randomTrack.TrackArtist?.Name, randomTrack.TrackNumber, randomTrack.ReleaseMedia.Release.ReleaseYear,
                                    TimeSpan.FromMilliseconds(SafeParser.ToNumber<double>(randomTrack.Duration)), isRated ? $"Rating: { randomTrack.Rating }" : randomTrack.PartTitles, randomTrack.LastUpdated ?? randomTrack.CreatedDate, ReleaseCoverArt(randomTrack.ReleaseMedia.Release.RoadieId));
                result.AddResource(t);
            }
            return result;
        }

        private IEnumerable<data.Track> RandomTracks(int randomLimit, short minimumRating)
        {
            var randomModels = (from t in DbContext.Tracks
                                join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                join r in DbContext.Releases on rm.ReleaseId equals r.Id
                                join a in DbContext.Artists on r.ArtistId equals a.Id
                                where t.Hash != null
                                where t.Rating >= minimumRating
                                select new TrackList
                                {
                                    DatabaseId = t.Id,
                                    Artist = new ArtistList
                                    {
                                        Artist = new DataToken { Value = a.RoadieId.ToString(), Text = a.Name }
                                    },
                                    Release = new ReleaseList
                                    {
                                        Release = new DataToken { Value = r.RoadieId.ToString(), Text = r.Title }
                                    }
                                })
                                .OrderBy(x => x.Artist.RandomSortId)
                                .ThenBy(x => x.RandomSortId)
                                .ThenBy(x => x.RandomSortId)
                                .Take(randomLimit)
                                .Select(x => x.DatabaseId)
                                .ToArray();

            return (from t in DbContext.Tracks
                                        .Include(x => x.TrackArtist)
                                        .Include(x => x.ReleaseMedia)
                                        .Include(x => x.ReleaseMedia.Release)
                                        .Include(x => x.ReleaseMedia.Release.Artist)
                                        .Include(x => x.ReleaseMedia.Release.Genres)
                                        .Include("ReleaseMedia.Release.Genres.Genre")
                    join rm in randomModels on t.Id equals rm
                    select t).ToArray();
        }

        private byte[] ReleaseCoverArt(Guid releaseId)
        {
            var imageResult = AsyncHelper.RunSync(() => ImageService.ReleaseImage(releaseId, 320, 320));
            return imageResult.Data?.Bytes;
        }

        private IEnumerable<string> ReleaseGroupKeys()
        {
            lock (lockObject)
            {
                return CacheManager.Get("urn:DlnaService:Releases", () =>
                {
                    return (from r in DbContext.Releases
                            select r)
                            .ToArray()
                            .Select(x => x.GroupBy)
                            .Distinct();

                }, "urn:DlnaServiceRegion");
            }
        }

        private IMediaFolder Releases()
        {
            var result = new VirtualFolder()
            {
                Name = "Releases",
                Id = "vf:releases"
            };
            foreach (var groupKey in ReleaseGroupKeys())
            {
                var f = new VirtualFolder(result, groupKey, $"vf:releasesforfolder:{ groupKey}");
                foreach (var releaseForGroup in ReleasesForGroup(groupKey))
                {
                    var af = new VirtualFolder(f, releaseForGroup.RoadieId.ToString(), $"vf:release:{ releaseForGroup.Id }");
                    f.AddFolder(af);
                }
                result.AddFolder(f);
            }

            return result;
        }

        private IEnumerable<data.Release> ReleasesForArtist(int artistId)
        {
            lock (lockObject)
            {
                return CacheManager.Get($"urn:DlnaService:ReleasesForArtist:{ artistId }", () =>
                {
                    return (from r in DbContext.Releases
                            where r.ArtistId == artistId
                            orderby r.ReleaseYear, r.SortTitleValue
                            select r).ToArray();
                }, "urn:DlnaServiceRegion");
            }
        }

        /// <summary>
        /// Return releases for an artist (level 4)
        /// </summary>
        private IMediaItem ReleasesForArtist(string id)
        {
            var artistId = SafeParser.ToNumber<int>(id.Replace("vf:artist:", ""));
            var artist = DbContext.Artists.FirstOrDefault(x => x.Id == artistId);
            var result = new VirtualFolder()
            {
                Name = artist.Name,
                Id = id
            };
            foreach (var artistRelease in ReleasesForArtist(artist.Id))
            {
                var fr = new VirtualFolder(result, artistRelease.Title, $"vf:release:{ artistRelease.Id }");
                foreach (var releaseTrack in TracksForRelease(artistRelease.Id))
                {
                    var t = new Track(releaseTrack.RoadieId.ToString(), releaseTrack.ReleaseMedia.Release.Artist.Name, releaseTrack.ReleaseMedia.Release.Title, releaseTrack.ReleaseMedia.MediaNumber,
                                      releaseTrack.Title, releaseTrack.ReleaseMedia.Release.Genres.Select(x => x.Genre.Name).ToCSV(), releaseTrack.TrackArtist?.Name, releaseTrack.TrackNumber, releaseTrack.ReleaseMedia.Release.ReleaseYear,
                                      TimeSpan.FromMilliseconds(SafeParser.ToNumber<double>(releaseTrack.Duration)), releaseTrack.PartTitles, releaseTrack.LastUpdated ?? releaseTrack.CreatedDate, null);
                    fr.AddResource(t);
                }
                result.AddFolder(fr);
            }
            return result;
        }

        private IEnumerable<data.Release> ReleasesForCollection(int collectionId)
        {
            lock (lockObject)
            {
                return CacheManager.Get($"urn:DlnaService:ReleasesForCollection:{ collectionId }", () =>
                {
                    return (from c in DbContext.Collections
                            join cr in DbContext.CollectionReleases on c.Id equals cr.CollectionId
                            join r in DbContext.Releases on cr.ReleaseId equals r.Id
                            where c.Id == collectionId
                            orderby cr.ListNumber, r.SortTitleValue
                            select r).ToArray();
                }, "urn:DlnaServiceRegion");
            }
        }

        private IMediaItem ReleasesForCollectionFolder(string id)
        {
            var collectionId = SafeParser.ToNumber<int>(id.Replace("vf:releasesforcollection:", ""));
            var collection = DbContext.Collections.FirstOrDefault(x => x.Id == collectionId);
            var result = new VirtualFolder()
            {
                Name = collection.Name,
                Id = id
            };
            foreach (var collectionRelease in ReleasesForCollection(collection.Id))
            {
                var fr = new VirtualFolder(result, collectionRelease.Title, $"vf:release:{ collectionRelease.Id }");
                foreach (var releaseTrack in TracksForRelease(collectionRelease.Id))
                {
                    var t = new Track(releaseTrack.RoadieId.ToString(), releaseTrack.ReleaseMedia.Release.Artist.Name, releaseTrack.ReleaseMedia.Release.Title, releaseTrack.ReleaseMedia.MediaNumber,
                                      releaseTrack.Title, releaseTrack.ReleaseMedia.Release.Genres.Select(x => x.Genre.Name).ToCSV(), releaseTrack.TrackArtist?.Name, releaseTrack.TrackNumber, releaseTrack.ReleaseMedia.Release.ReleaseYear,
                                      TimeSpan.FromMilliseconds(SafeParser.ToNumber<double>(releaseTrack.Duration)), releaseTrack.PartTitles, releaseTrack.LastUpdated ?? releaseTrack.CreatedDate, null);
                    fr.AddResource(t);
                }
                result.AddFolder(fr);
            }
            return result;
        }

        /// <summary>
        /// Returns releases for group letter (level 3)
        /// </summary>
        private IMediaItem ReleasesForFolder(string id)
        {
            var artistsForFolderKey = id.Replace("vf:releasesforfolder:", "");
            var result = new VirtualFolder()
            {
                Name = artistsForFolderKey,
                Id = id
            };

            foreach (var releaseForGroup in ReleasesForGroup(artistsForFolderKey))
            {
                var af = new VirtualFolder(result, releaseForGroup.Title, $"vf:release:{ releaseForGroup.Id }");
                foreach (var artistRelease in TracksForRelease(releaseForGroup.Id))
                {
                    var fr = new VirtualFolder(af, artistRelease.RoadieId.ToString(), $"vf:release:{ artistRelease.Id }");
                    af.AddFolder(fr);
                }
                result.AddFolder(af);
            }
            return result;
        }

        private IEnumerable<data.Release> ReleasesForGroup(string groupKey)
        {
            lock (lockObject)
            {
                return CacheManager.Get($"urn:DlnaService:ReleasesForGroup:{ groupKey }", () =>
                {
                    var sw = Stopwatch.StartNew();
                    var result = DbContext.Releases.AsEnumerable().Where(x => x.GroupBy == groupKey).Distinct().ToArray();
                    sw.Stop();
                    Logger.LogDebug($"DLNA ReleasesForGroup [{ groupKey }] Elapsed Time [{ sw.Elapsed }]");
                    return result;
                }, "urn:DlnaServiceRegion");
            }
        }

        /// <summary>
        /// Returns items to display at top level (level 1)
        /// </summary>
        /// <returns></returns>
        private IMediaFolder RootFolder()
        {
            var result = new VirtualFolder();
            result.AddFolder(Artists());
            result.AddFolder(Collections());
            result.AddFolder(Playlists());
            result.AddFolder(Randomizer());
            result.AddFolder(Releases());
            return result;
        }

        private bool ShouldMakeScrobble(string trackToken)
        {
            if (!LastTimePlayedForToken.ContainsKey(trackToken))
            {
                LastTimePlayedForToken.Add(trackToken, DateTime.UtcNow);
            }
            return (DateTime.UtcNow - LastTimePlayedForToken[trackToken]).TotalMilliseconds < 1000;
        }

        private async Task<byte[]> TrackBytesAndMarkPlayed(int releaseId, data.Track track, string trackToken)
        {
            var results = await TrackService.TrackStreamInfo(track.RoadieId, 0, SafeParser.ToNumber<long>(track.FileSize), null).ConfigureAwait(false);
            // Some DLNA clients call for the track file several times for each play
            if (ShouldMakeScrobble(trackToken))
            {
                await PlayActivityService.Scrobble(null, new Library.Scrobble.ScrobbleInfo
                {
                    TrackId = track.RoadieId,
                    TimePlayed = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            return results.Data.Bytes;
        }

        private IMediaItem TrackDetail(string id, bool isFileRequest)
        {
            lock (lockObject)
            {
                var releaseId = SafeParser.ToNumber<int>(id.Replace("r:t:tk:", "").Split(':')[0]);
                var trackId = SafeParser.ToNumber<int>(id.Replace("r:t:tk:", "").Split(':')[1]);
                var trackToken = id.Replace("r:t:tk:", "").Split(':')[2];

                var track = TracksForRelease(releaseId).First(x => x.Id == trackId);

                byte[] trackbytes = null;
                if (isFileRequest)
                {
                    trackbytes = AsyncHelper.RunSync(() => TrackBytesAndMarkPlayed(releaseId, track, trackToken));
                }
                return new Track($"r:t:tk:{releaseId}:{trackId}:{ Guid.NewGuid() }", track.ReleaseMedia.Release.Artist.Name, track.ReleaseMedia.Release.Title, track.ReleaseMedia.MediaNumber,
                                    track.Title, track.ReleaseMedia.Release.Genres.Select(x => x.Genre.Name).ToCSV(), track.TrackArtist?.Name,
                                    track.TrackNumber, track.ReleaseMedia.Release.ReleaseYear, TimeSpan.FromMilliseconds(SafeParser.ToNumber<double>(track.Duration)),
                                    track.PartTitles, track.LastUpdated ?? track.CreatedDate, ReleaseCoverArt(track.ReleaseMedia.Release.RoadieId), trackbytes);
            }
        }

        private IEnumerable<data.Track> TracksForPlaylist(int playlistId)
        {
            lock (lockObject)
            {
                return CacheManager.Get($"urn:DlnaService:TracksForPlaylist:{ playlistId }", () =>
                {
                    return (from pl in DbContext.Playlists
                            join plr in DbContext.PlaylistTracks on pl.Id equals plr.PlayListId
                            join t in DbContext.Tracks.Include(x => x.TrackArtist)
                                                      .Include(x => x.ReleaseMedia)
                                                      .Include(x => x.ReleaseMedia.Release)
                                                      .Include(x => x.ReleaseMedia.Release.Artist)
                                                      .Include(x => x.ReleaseMedia.Release.Genres)
                                                      .Include("ReleaseMedia.Release.Genres.Genre") on plr.TrackId equals t.Id
                            join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                            where pl.Id == playlistId
                            orderby plr.ListNumber
                            select t).ToArray();
                }, "urn:DlnaServiceRegion");
            }
        }

        private IMediaItem TracksForPlaylist(string id)
        {
            var playlistId = SafeParser.ToNumber<int>(id.Replace("vf:tracksforplaylist:", ""));
            var playlist = DbContext.Playlists.FirstOrDefault(x => x.Id == playlistId);
            var result = new VirtualFolder()
            {
                Name = playlist.Name,
                Id = id
            };

            foreach (var playlistTrack in TracksForPlaylist(playlist.Id))
            {
                var t = new Track($"r:t:tk:{playlistTrack.ReleaseMedia.Release.Id}:{playlistTrack.Id}:{ Guid.NewGuid() }", playlistTrack.ReleaseMedia.Release.Artist.Name, playlistTrack.ReleaseMedia.Release.Title, playlistTrack.ReleaseMedia.MediaNumber,
                                    playlistTrack.Title, playlistTrack.ReleaseMedia.Release.Genres.Select(x => x.Genre.Name).ToCSV(), playlistTrack.TrackArtist?.Name, playlistTrack.TrackNumber, playlistTrack.ReleaseMedia.Release.ReleaseYear,
                                    TimeSpan.FromMilliseconds(SafeParser.ToNumber<double>(playlistTrack.Duration)), playlistTrack.PartTitles, playlistTrack.LastUpdated ?? playlistTrack.CreatedDate, ReleaseCoverArt(playlistTrack.ReleaseMedia.Release.RoadieId));
                result.AddResource(t);
            }
            return result;
        }

        private IEnumerable<data.Track> TracksForRelease(int releaseId)
        {
            lock (lockObject)
            {
                return CacheManager.Get($"urn:DlnaService:TracksForRelease:{ releaseId }", () =>
                {
                    return (from t in DbContext.Tracks
                                               .Include(x => x.TrackArtist)
                                               .Include(x => x.ReleaseMedia)
                                               .Include(x => x.ReleaseMedia.Release)
                                               .Include(x => x.ReleaseMedia.Release.Artist)
                                               .Include(x => x.ReleaseMedia.Release.Genres)
                                               .Include("ReleaseMedia.Release.Genres.Genre")
                            join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                            where rm.ReleaseId == releaseId
                            orderby rm.MediaNumber, t.TrackNumber
                            select t).ToArray();
                }, "urn:DlnaServiceRegion");
            }
        }

        private IMediaItem TracksForRelease(string id)
        {
            var releaseId = SafeParser.ToNumber<int>(id.Replace("vf:release:", ""));
            var release = DbContext.Releases.FirstOrDefault(x => x.Id == releaseId);
            var result = new VirtualFolder()
            {
                Name = release.Title,
                Id = id
            };

            foreach (var releaseTrack in TracksForRelease(release.Id))
            {
                var t = new Track($"r:t:tk:{release.Id}:{releaseTrack.Id}:{Guid.NewGuid()}", releaseTrack.ReleaseMedia.Release.Artist.Name, releaseTrack.ReleaseMedia.Release.Title, releaseTrack.ReleaseMedia.MediaNumber,
                                    releaseTrack.Title, releaseTrack.ReleaseMedia.Release.Genres.Select(x => x.Genre.Name).ToCSV(), releaseTrack.TrackArtist?.Name,
                                    releaseTrack.TrackNumber, releaseTrack.ReleaseMedia.Release.ReleaseYear, TimeSpan.FromMilliseconds(SafeParser.ToNumber<double>(releaseTrack.Duration)),
                                    releaseTrack.PartTitles, releaseTrack.LastUpdated ?? releaseTrack.CreatedDate, ReleaseCoverArt(release.RoadieId));
                result.AddResource(t);
            }
            return result;
        }
    }
}