namespace Roadie.Library.Data
{
    public partial class Bookmark
    {
        public override string ToString()
        {
            return $"Id [{Id}], BookmarkType [{BookmarkType}], BookmarkTargetId [{BookmarkTargetId}]";
        }
    }
}