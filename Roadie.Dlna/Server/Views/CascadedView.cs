using Roadie.Dlna.Utility;
using System.Diagnostics;
using System.Linq;

namespace Roadie.Dlna.Server.Views
{
    internal abstract class CascadedView : BaseView, IConfigurable
    {
        private bool cascade = true;

        public void SetParameters(ConfigParameters parameters)
        {
            cascade = !parameters.Has("no-cascade") && parameters.Get("cascade", cascade);
        }

        public override IMediaFolder Transform(IMediaFolder oldRoot)
        {
            var root = new VirtualClonedFolder(oldRoot);
            var sorted = new SimpleKeyedVirtualFolder(root, Name);
            SortFolder(root, sorted);
            Trace.WriteLine($"sort {sorted.ChildFolders.Count()} - {sorted.ChildItems.Count()}");
            Trace.WriteLine($"root {root.ChildFolders.Count()} - {root.ChildItems.Count()}");
            foreach (var f in sorted.ChildFolders.ToList())
            {
                if (f.ChildCount < 2)
                {
                    foreach (var file in f.ChildItems)
                    {
                        root.AddResource(file);
                    }
                    continue;
                }
                var fsmi = f as VirtualFolder;
                root.AdoptFolder(fsmi);
            }
            foreach (var f in sorted.ChildItems.ToList())
            {
                root.AddResource(f);
            }
            Trace.WriteLine($"merg {root.ChildFolders.Count()} - {root.ChildItems.Count()}");
            MergeFolders(root, root);
            Trace.WriteLine($"done {root.ChildFolders.Count()} - {root.ChildItems.Count()}");

            if (!cascade || root.ChildFolders.LongCount() <= 50)
            {
                return root;
            }
            var cascaded = new DoubleKeyedVirtualFolder(root, "Series");
            foreach (var i in root.ChildFolders.ToList())
            {
                var folder = cascaded.GetFolder(i.Title.StemCompareBase().Substring(0, 1).ToUpper());
                folder.AdoptFolder(i);
            }
            foreach (var i in root.ChildItems.ToList())
            {
                var folder = cascaded.GetFolder(i.Title.StemCompareBase().Substring(0, 1).ToUpper());
                folder.AddResource(i);
            }
            return cascaded;
        }

        protected abstract void SortFolder(IMediaFolder folder,
                      SimpleKeyedVirtualFolder series);
    }
}