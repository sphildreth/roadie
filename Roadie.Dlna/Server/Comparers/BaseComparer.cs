namespace Roadie.Dlna.Server.Comparers
{
    internal abstract class BaseComparer
    {
        public abstract string Description { get; }

        public abstract string Name { get; }

        public abstract int Compare(IMediaItem x, IMediaItem y);

        public override string ToString() => $"{Name} - {Description}";
    }
}