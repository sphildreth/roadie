using System;

namespace Roadie.Library.Models
{
    [Serializable]
    public class LabelList : EntityInfoModelBase
    {
        public int? ArtistCount { get; set; }
        public DataToken Label { get; set; }
        public int? ReleaseCount { get; set; }
        public Image Thumbnail { get; set; }
        public int? TrackCount { get; set; }

        public static LabelList FromDataLabel(Data.Label label, Image labelThumbnail)
        {
            return new LabelList
            {
                Id = label.RoadieId,
                Label = new DataToken
                {
                    Text = label.Name,
                    Value = label.RoadieId.ToString()
                },
                SortName = label.SortName,
                CreatedDate = label.CreatedDate,
                LastUpdated = label.LastUpdated,
                ArtistCount = label.ArtistCount,
                ReleaseCount = label.ReleaseCount,
                TrackCount = label.TrackCount,
                Thumbnail = labelThumbnail
            };
        }
    }
}