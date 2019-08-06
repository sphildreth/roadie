using Mapster;
using Newtonsoft.Json;
using Roadie.Library.Utility;
using System;

namespace Roadie.Library.Models
{
    /// <summary>
    ///     Generic Data "Token" (or List Item) for associations and child lists on objects. Example "Genres" for a Release.
    /// </summary>
    [Serializable]
    public class DataToken
    {
        /// <summary>
        ///     Any specific tooltip if none given returns Text
        /// </summary>
        private string _tooltip;

        /// <summary>
        ///     Any specific or special Css to apply to the item
        /// </summary>
        public string CssClass { get; set; }

        public object Data { get; set; }

        /// <summary>
        ///     Is the item disabled
        /// </summary>
        public bool? Disabled { get; set; }

        /// <summary>
        ///     Is the item selected
        /// </summary>
        public bool? Selected { get; set; }

        /// <summary>
        ///     This is the Text to show to the user (ie name of genre or artist or label)
        /// </summary>
        public string Text { get; set; }

        public string Tooltip
        {
            get => _tooltip ?? Text;
            set => _tooltip = value;
        }

        /// <summary>
        ///     This is the value to submit or the Key (Guid) of the item
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        ///     Random int to sort when Random Request
        /// </summary>
        [AdaptIgnore]
        [JsonIgnore]
        public int RandomSortId { get; set; }

        public DataToken()
        {
            RandomSortId = StaticRandom.Instance.Next();
        }

        public override string ToString()
        {
            return $"Text [{Text}], Value [{Value}]";
        }
    }
}