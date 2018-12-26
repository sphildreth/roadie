using System;

namespace Roadie.Library.Configuration
{
    /// <summary>
    /// This is a Api Key used by Roadie to interact with an API (ie KeyName is "BingImageSearch" and its key is the BingImageSearch Key)
    /// </summary>
    [Serializable]
    public class ApiKey
    {
        public string ApiName { get; set; }
        public string Key { get; set; }
        public string KeySecret { get; set; }
    }
}