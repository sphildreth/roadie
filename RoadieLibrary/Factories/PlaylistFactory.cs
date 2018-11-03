using Roadie.Library.Caching;
using MySql.Data.MySqlClient;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.Processors;
using Roadie.Library.Utility;
using Roadie.Library.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Roadie.Library.Data;
using Microsoft.Extensions.Configuration;

namespace Roadie.Library.Factories
{
    public class PlaylistFactory : FactoryBase
    {
        public PlaylistFactory(IConfiguration configuration, IRoadieDbContext context, ICacheManager cacheManager, ILogger logger) : base(configuration, context, cacheManager, logger)
        {
        }

        public async Task<FactoryResult<bool>> AddTracksToPlaylist(Playlist playlist, IEnumerable<string> trackIds)
        {
            var sw = new Stopwatch();
            sw.Start();

            var result = false;
            var now = DateTime.UtcNow;

            var existingTracksForPlaylist = (from plt in this.DbContext.PlaylistTracks
                                             join t in this.DbContext.Tracks on plt.TrackId equals t.Id
                                             where plt.PlayListId == playlist.Id
                                             select t);
            var newTracksForPlaylist = (from t in this.DbContext.Tracks
                                        where (from x in trackIds select x).Contains(t.RoadieId)
                                        where !(from x in existingTracksForPlaylist select x.RoadieId).Contains(t.RoadieId)
                                        select t).ToArray();
            foreach (var newTrackForPlaylist in newTracksForPlaylist)
            {
                this.DbContext.PlaylistTracks.Add(new PlaylistTrack
                {
                    TrackId = newTrackForPlaylist.id,
                    PlayListId = playlist.Id,
                    CreatedDate = now,
                    RoadieId = Guid.NewGuid()
                });
            }
            playlist.LastUpdated = now;
            await this.DbContext.SaveChangesAsync();
            result = true;

            var r = await this.ReorderPlaylist(playlist);
            result = result && r.IsSuccess;

            return new FactoryResult<bool>
            {
                Data = result
            };
        }

        public async Task<FactoryResult<bool>> ReorderPlaylist(Playlist playlist)
        {
            var sw = new Stopwatch();
            sw.Start();

            var result = false;
            var now = DateTime.UtcNow;

            if (playlist != null)
            {
                var looper = 0;
                foreach(var playlistTrack in this.DbContext.PlaylistTracks.Where(x => x.PlayListId == playlist.Id).OrderBy(x => x.createdDate))
                {
                    looper++;
                    playlistTrack.ListNumber = looper;
                    playlistTrack.LastUpdated = now;
                }
                await this.DbContext.SaveChangesAsync();
                result = true;
            }

            return new FactoryResult<bool>
            {
                IsSuccess = result,
                Data = result
            };

        }


    }
}
