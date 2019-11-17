using System.Threading.Tasks;

namespace Roadie.Library.Data
{
    public interface IRoadieDbUserStats
    {
        Task<Artist> MostPlayedArtist(int userId);

        Task<Release> MostPlayedRelease(int userId);

        Task<Track> MostPlayedTrack(int userId);

        Task<Artist> LastPlayedArtist(int userId);

        Task<Release> LastPlayedRelease(int userId);

        Task<Track> LastPlayedTrack(int userId);
    }
}