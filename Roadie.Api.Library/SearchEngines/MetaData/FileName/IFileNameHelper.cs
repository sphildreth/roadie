using Roadie.Library.MetaData.Audio;
using System.IO;

namespace Roadie.Library.MetaData.FileName
{
    public interface IFileNameHelper
    {
        string CleanString(string input);

        AudioMetaData MetaDataFromFileInfo(FileInfo fileInfo);

        AudioMetaData MetaDataFromFilename(string rawFilename);
    }
}
