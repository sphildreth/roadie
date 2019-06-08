using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.SearchEngines.MetaData.LastFm
{
    public class LastFmApiException : Exception
    {
        /// <summary>
        /// Instantiates a Last.fm API exception
        /// </summary>
        public LastFmApiException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Instantiates a Last.fm API exception
        /// </summary>
        public LastFmApiException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
