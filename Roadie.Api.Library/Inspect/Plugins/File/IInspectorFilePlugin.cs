using Roadie.Library.MetaData.Audio;

namespace Roadie.Library.Inspect.Plugins.File
{
    public interface IInspectorFilePlugin
    {
        bool IsEnabled { get; }
        string Description { get; }
        int Order { get; }

        OperationResult<AudioMetaData> Process(AudioMetaData metaData);
    }
}