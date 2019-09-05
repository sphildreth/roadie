using Microsoft.IO;
using System.IO;

namespace Roadie.Dlna.Utility
{
    public static class StreamManager
    {
        private static readonly RecyclableMemoryStreamManager manager = new RecyclableMemoryStreamManager();

        public static MemoryStream GetStream()
        {
            return manager.GetStream();
        }

        public static MemoryStream GetStream(string tag)
        {
            return manager.GetStream(tag);
        }
    }
}