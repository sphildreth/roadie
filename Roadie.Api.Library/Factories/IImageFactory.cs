using Roadie.Library.MetaData.Audio;

namespace Roadie.Library.Factories
{
    public interface IImageFactory
    {
        AudioMetaDataImage GetPictureForMetaData(string filename, AudioMetaData metaData);

        AudioMetaDataImage ImageForFilename(string filename);
    }
}