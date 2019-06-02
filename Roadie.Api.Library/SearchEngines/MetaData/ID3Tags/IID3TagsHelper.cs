using System.Collections.Generic;
using Roadie.Library.MetaData.Audio;

namespace Roadie.Library.MetaData.ID3Tags
{
    public interface IID3TagsHelper
    {
        OperationResult<AudioMetaData> MetaDataForFile(string fileName, bool returnEvenIfInvalid = false);
        OperationResult<IEnumerable<AudioMetaData>> MetaDataForFiles(IEnumerable<string> fileNames);
        OperationResult<IEnumerable<AudioMetaData>> MetaDataForFolder(string folderName);
        bool WriteTags(AudioMetaData metaData, string filename, bool force = false);
    }
}