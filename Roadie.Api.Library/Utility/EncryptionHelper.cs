using System;
using System.IO;
using System.Security.Cryptography;

namespace Roadie.Library.Utility
{
    public static class EncryptionHelper
    {
        public static string Decrypt(string cyphertext, string key)
        {
            if (string.IsNullOrEmpty(cyphertext) || string.IsNullOrEmpty(key)) return null;
            if (key.Length > 16) key = key.Substring(0, 16);
            return Decrypt(Convert.FromBase64String(cyphertext), System.Text.Encoding.UTF8.GetBytes(key));
        }

        public static string Decrypt(byte[] cyphertext, byte[] key)
        {
            using (var ms = new MemoryStream(cyphertext))
            using (var desObj = Rijndael.Create())
            {
                desObj.Key = key;
                var iv = new byte[16];
                var offset = 0;
                while (offset < iv.Length) offset += ms.Read(iv, offset, iv.Length - offset);
                desObj.IV = iv;
                using (var cs = new CryptoStream(ms, desObj.CreateDecryptor(), CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs, System.Text.Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        public static string Encrypt(string plaintext, string key)
        {
            if (string.IsNullOrEmpty(plaintext) || string.IsNullOrEmpty(key)) return null;
            if (key.Length > 16) key = key.Substring(0, 16);
            return Convert.ToBase64String(Encrypt(plaintext, System.Text.Encoding.UTF8.GetBytes(key)));
        }

        public static byte[] Encrypt(string plaintext, byte[] key)
        {
            using (var desObj = Rijndael.Create())
            {
                desObj.Key = key;
                using (var ms = new MemoryStream())
                {
                    ms.Write(desObj.IV, 0, desObj.IV.Length);
                    using (var cs = new CryptoStream(ms, desObj.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
                        cs.Write(plainTextBytes, 0, plainTextBytes.Length);
                    }

                    return ms.ToArray();
                }
            }
        }
    }
}