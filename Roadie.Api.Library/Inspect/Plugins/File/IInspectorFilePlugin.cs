using Roadie.Library.MetaData.Audio;

namespace Roadie.Library.Inspect.Plugins.File
{
    public interface IInspectorFilePlugin
    {
        string Description { get; }

        bool IsEnabled { get; }

        int Order { get; }

        OperationResult<AudioMetaData> Process(AudioMetaData metaData);
    }
}
