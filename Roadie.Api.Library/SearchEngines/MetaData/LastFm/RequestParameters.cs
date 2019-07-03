using System.Collections.Generic;
using System.Text;
using System.Web;

namespace Roadie.Library.SearchEngines.MetaData.LastFm
{
    internal class RequestParameters : SortedDictionary<string, string>
    {
        /// <summary>
        ///     The Name Value Pair Format String used by this object
        /// </summary>
        public const string NameValuePairStringFormat = "{0}={1}";

        /// <summary>
        ///     The Name-value pair seperator used by this object
        /// </summary>
        public const string NameValuePairStringSeperator = "&";

        public RequestParameters()
        {
        }

        internal RequestParameters(string serialization)
        {
            var values = serialization.Split('\t');

            for (var i = 0; i < values.Length - 1; i++)
                if (i % 2 == 0)
                    this[values[i]] = values[i + 1];
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            var count = 0;
            foreach (var key in Keys)
            {
                if (count > 0) builder.Append(NameValuePairStringSeperator);
                builder.AppendFormat(NameValuePairStringFormat, key, HttpUtility.UrlEncode(this[key]));
                count++;
            }

            return builder.ToString();
        }

        internal string serialize()
        {
            var line = "";

            foreach (var key in Keys)
                line += key + "\t" + this[key] + "\t";

            return line;
        }

        internal byte[] ToBytes()
        {
            return System.Text.Encoding.UTF8.GetBytes(ToString());
        }
    }
}