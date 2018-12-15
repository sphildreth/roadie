using System.IO;
using Roadie.Library.MetaData.Audio;

namespace Roadie.Library.MetaData.FileName
{
    public interface IFileNameHelper
    {
        string CleanString(string input);
        AudioMetaData MetaDataFromFileInfo(FileInfo fileInfo);
        AudioMetaData MetaDataFromFilename(string rawFilename);
    }
}