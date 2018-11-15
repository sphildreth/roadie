using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models
{
    /// <summary>
    /// Generic Data "Token" (or List Item) for associations and child lists on objects. Example "Genres" for a Release.
    /// </summary>
    [Serializable]
    public class DataToken
    {
        /// <summary>
        /// This is the Text to show to the user (ie name of genre or artist or label)
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// This is the value to submit or the Key (Guid) of the item
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Is the item selected
        /// </summary>
        public bool? Selected { get; set; }
        /// <summary>
        /// Is the item disabled
        /// </summary>
        public bool? Disabled { get; set; }
        /// <summary>
        /// Any specific or special Css to apply to the item
        /// </summary>
        public string CssClass { get; set; }
        /// <summary>
        /// Any specific tooltip if none given returns Text
        /// </summary>
        private string _tooltip = null;
        public string Tooltip
        {
            get
            {
                return this._tooltip ?? this.Text;
            }
            set
            {
                this._tooltip = value;
            }
        }
        public object Data { get; set; }
    }
}
