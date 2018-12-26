using System;

namespace Roadie.Library.MetaData.Audio
{
    [Flags]
    public enum AudioMetaDataWeights
    {
        None = 0,
        Year = 1,
        Time = 2,
        TrackNumber = 4,
        Release = 8,
        Title = 16,
        Artist = 32
    }

    //Artist + Release + TrackTitle 56
    //Artist + Release + TrackNumber 44
    //Artist + TrackNumber + Title 38
    //Artist + Release + TrackNumber + TrackTitle = 60
}