using System;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public class ReplacementString : IReplacementString
    {
        public string Key { get; set; }
        public int Order { get; set; }
        public string ReplaceWith { get; set; }
    }
}