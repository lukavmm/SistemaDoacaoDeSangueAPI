using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SistemaDoacaoSangue.Functions
{
    public class Encryption
    {
        private static string HashEncode(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
        private static byte[] HexDecode(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
            }
            return bytes;
        }
        private static byte[] StringEncode(string text)
        {
            var encoding = new ASCIIEncoding();
            return encoding.GetBytes(text);
        }
        private static byte[] HashHMAC(byte[] key, byte[] message)
        {
            var hash = new HMACSHA256(key);
            return hash.ComputeHash(message);
        }
        public static string HashHMACHex(string keyHex, string message)
        {
            byte[] hash = HashHMAC(HexDecode(keyHex), StringEncode(message));
            return HashEncode(hash);
        }
    }

    class EncryptDecrypt
    {
        public byte[] Key { get; set; }
        public byte[] IniVetor { get; set; }
        public Aes Algorithm { get; set; }

        public EncryptDecrypt(byte[] key)
        {
            Key = key;
            IniVetor = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            Algorithm = Aes.Create();
        }

        public EncryptDecrypt(byte[] key, byte[] iniVetor)
        {
            Key = key;
            IniVetor = iniVetor;
            Algorithm = Aes.Create();
        }

        public string Encrypt(string entryText)
        {
            byte[] symEncryptedData;

            var dataToProtectAsArray = Encoding.UTF8.GetBytes(entryText);
            using (var encryptor = Algorithm.CreateEncryptor(Key, IniVetor))
            using (var memoryStream = new MemoryStream())
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(dataToProtectAsArray, 0, dataToProtectAsArray.Length);
                cryptoStream.FlushFinalBlock();
                symEncryptedData = memoryStream.ToArray();
            }
            Algorithm.Dispose();
            return Convert.ToBase64String(symEncryptedData);
        }

        public string Decrypt(string entryText)
        {
            var symEncryptedData = Convert.FromBase64String(entryText);
            byte[] symUnencryptedData;
            using (var decryptor = Algorithm.CreateDecryptor(Key, IniVetor))
            using (var memoryStream = new MemoryStream())
            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(symEncryptedData, 0, symEncryptedData.Length);
                cryptoStream.FlushFinalBlock();
                symUnencryptedData = memoryStream.ToArray();
            }
            Algorithm.Dispose();
            return Encoding.Default.GetString(symUnencryptedData);
        }
    }
}
