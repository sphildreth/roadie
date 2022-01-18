using System;
using System.Linq;
using System.Security.Cryptography;

namespace Roadie.Library.Utility
{
    public static class EncryptionHelper
    {

        public static string Decrypt(string cyphertext, string key)
        {
            if (string.IsNullOrEmpty(cyphertext) || string.IsNullOrEmpty(key))
            {
                return null;
            }
            if (key.Length > 16)
            {
                key = key.Substring(0, 16);
            }
            return Decrypt(Convert.FromBase64String(cyphertext), System.Text.Encoding.UTF8.GetBytes(key));
        }

        public static string Decrypt(byte[] encryptedData, byte[] key)
        {
            return SymmetricEncryptor.DecryptToString(encryptedData, key);
        }

        public static string Encrypt(string plaintext, string key)
        {
            if (string.IsNullOrEmpty(plaintext) || string.IsNullOrEmpty(key))
            {
                return null;
            }
            if (key.Length > 16)
            {
                key = key.Substring(0, 16);
            }
            return Convert.ToBase64String(Encrypt(plaintext, System.Text.Encoding.UTF8.GetBytes(key)));
        }

        public static byte[] Encrypt(string toEncrypt, byte[] key)
        {
            return SymmetricEncryptor.EncryptString(toEncrypt, key);
        }
    }
}
