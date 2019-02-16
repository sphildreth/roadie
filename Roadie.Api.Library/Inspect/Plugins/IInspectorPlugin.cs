using Roadie.Library.MetaData.Audio;
using System.Collections.Generic;
using System.IO;

namespace Roadie.Library.Inspect.Plugins
{
    public interface IInspectorPlugin
    {
        string Description { get; }
        int Order { get; }

        OperationResult<IEnumerable<AudioMetaData>> Process(IEnumerable<AudioMetaData> metaDatas);
    }
}