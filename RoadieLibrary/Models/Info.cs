using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Data.Models
{
    /// <summary>
    /// Generic Info for a child item on a Detail record. Like a Label on a Release, or an Artist on a Release, or a Label for an Artist
    /// </summary>
    [Serializable]
    public class Info
    {
        /// <summary>
        /// The Id for the Info (like ArtistId, Or ReleaseId)
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Display name for the Info (like Artist Name or Release Title)
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Any tooltip to use with the DisplayName 
        /// </summary>
        public string Tooltip { get; set; }
        /// <summary>
        /// Url to the Image to show for the Info 
        /// </summary>
        public string ImageUrl { get; set; }
        /// <summary>
        /// Url to see full details
        /// </summary>
        public string DetailUrl { get; set; }
        /// <summary>
        /// Any CSS class to apply
        /// </summary>
        public string CssClass { get; set; }
    }
}
