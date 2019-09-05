using System;
using System.Collections.Generic;
using System.IO;

namespace Roadie.Dlna.Utility
{
    public static class ResourceHelper
    {
        private static object _sybcRoot = new object();

        private static Dictionary<string, byte[]> _cache = new Dictionary<string, byte[]>();

        public static byte[] GetResourceData(string resource)
        {
            lock (_sybcRoot)
            {
                if (!_cache.ContainsKey(resource))
                {
                    var pathToResourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Server", "Resources", resource);
                    _cache.Add(resource, File.ReadAllBytes(pathToResourceFile));
                }
                return _cache[resource];
            }
        }
    }
}