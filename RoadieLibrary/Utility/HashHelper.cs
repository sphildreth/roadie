using Roadie.Library.Encoding;
using System.Linq;
using System.Security.Cryptography;

namespace Roadie.Library.Utility
{
    public static class HashHelper
    {
        public static string CreateMD5(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            return CreateMD5(System.Text.Encoding.ASCII.GetBytes(input));
        }

        public static string CreateMD5(byte[] bytes)
        {
            if (bytes == null || !bytes.Any())
            {
                return null;
            }
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                return System.Text.Encoding.ASCII.GetString(md5.ComputeHash(bytes));
            }
        }
    }
}