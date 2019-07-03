using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Roadie.Library.Utility
{
    public static class HashHelper
    {
        public static string CreateMD5(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            return CreateMD5(System.Text.Encoding.ASCII.GetBytes(input));
        }

        public static string CreateMD5(byte[] bytes)
        {
            if (bytes == null || !bytes.Any()) return null;
            using (var md5 = MD5.Create())
            {
                return System.Text.Encoding.ASCII.GetString(md5.ComputeHash(bytes));
            }
        }

        public static string MD5Hash(string input)
        {
            var hash = new StringBuilder();
            var md5provider = new MD5CryptoServiceProvider();
            var bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            for (var i = 0; i < bytes.Length; i++) hash.Append(bytes[i].ToString("x2"));
            return hash.ToString();
        }
    }
}