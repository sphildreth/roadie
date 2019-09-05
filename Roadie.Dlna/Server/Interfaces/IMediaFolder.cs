using System.Collections.Generic;

namespace Roadie.Dlna.Server
{
    public interface IMediaFolder : IMediaItem
    {
        int ChildCount { get; }

        IEnumerable<IMediaFolder> ChildFolders { get; }
        IEnumerable<IMediaResource> ChildItems { get; }
        int FullChildCount { get; }
        IMediaFolder Parent { get; set; }

        void AddResource(IMediaResource res);

        void Cleanup();

        bool RemoveResource(IMediaResource res);

        void Sort(IComparer<IMediaItem> sortComparer, bool descending);
    }
}