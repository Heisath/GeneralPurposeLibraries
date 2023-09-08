using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.Json;

namespace NetCoreNetwork.Shared
{
    public class CryptLibrary
    {
        private readonly Aes aes;
        private bool initialized;

        public CryptLibrary()
        {
            aes = Aes.Create("AesManaged") ?? throw new CryptographicException("AesManaged not available!");
        }

        public byte[] EncryptBuffer(byte[] buffer)
        {
            if (!initialized) throw new InvalidOperationException("Encryption not initialized!");
            return EncryptBytes(aes, buffer);
        }
        public byte[] DecryptBuffer(byte[] buffer)
        {
            if (!initialized) throw new InvalidOperationException("Encryption not initialized!");
            return DecryptBytes(aes, buffer);
        }
                
        public void PerformServerSideKeyExchange(Stream stream)
        {
            // Generate RSA KeyPair and export public key
            using RSACryptoServiceProvider rsa = new(4096);
            byte[] PublicRSAKey = rsa.ExportRSAPublicKey();
            
            // send public key to client
            byte[] size = BitConverter.GetBytes(PublicRSAKey.Length);
            stream.Write(size, 0, size.Length);
            stream.Write(PublicRSAKey, 0, PublicRSAKey.Length);
            stream.Flush();

            // read encrypted symmetric key from client
            byte[] buf = new byte[4];
            stream.Read(buf, 0, buf.Length);
            buf = new byte[BitConverter.ToInt32(buf, 0)];
            stream.Read(buf, 0, buf.Length);
            aes.Key = rsa.Decrypt(buf, false);

            // read encrypted IV from client
            buf = new byte[4];
            stream.Read(buf, 0, buf.Length);
            buf = new byte[BitConverter.ToInt32(buf, 0)];
            stream.Read(buf, 0, buf.Length);
            aes.IV = rsa.Decrypt(buf, false);

            initialized = true;
        }

        public void PerformClientSideKeyExchange(Stream stream)
        {
            byte[] buf = new byte[4];
            stream.Read(buf, 0, buf.Length);

            byte[] PublicRSAKey = new byte[BitConverter.ToInt32(buf, 0)];
            stream.Read(PublicRSAKey, 0, PublicRSAKey.Length);

            aes.GenerateIV();
            aes.GenerateKey();

            using RSACryptoServiceProvider rsa = new();
            rsa.ImportRSAPublicKey(PublicRSAKey, out _);

            byte[] EncryptedKey = rsa.Encrypt(aes.Key, false);
            byte[] EncryptedIV = rsa.Encrypt(aes.IV, false);

            byte[] size = BitConverter.GetBytes(EncryptedKey.Length);
            stream.Write(size, 0, size.Length);
            stream.Write(EncryptedKey, 0, EncryptedKey.Length);
            stream.Flush();

            size = BitConverter.GetBytes(EncryptedIV.Length);
            stream.Write(size, 0, size.Length);
            stream.Write(EncryptedIV, 0, EncryptedIV.Length);
            stream.Flush();

            initialized = true;
        }

        

        private static byte[] EncryptBytes(SymmetricAlgorithm alg, byte[] message)
        {
            if (message.Length == 0)
            {
                return message;
            }

            using var stream = new MemoryStream();
            using var encryptor = alg.CreateEncryptor();
            using var encrypt = new CryptoStream(stream, encryptor, CryptoStreamMode.Write);

            encrypt.Write(message, 0, message.Length);
            encrypt.FlushFinalBlock();
            return stream.ToArray();
        }
        private static byte[] DecryptBytes(SymmetricAlgorithm alg, byte[] message)
        {
            if (message.Length == 0)
            {
                return message;
            }

            using var stream = new MemoryStream();
            using var decryptor = alg.CreateDecryptor();
            using var encrypt = new CryptoStream(stream, decryptor, CryptoStreamMode.Write);

            encrypt.Write(message, 0, message.Length);
            encrypt.FlushFinalBlock();
            return stream.ToArray();
        }
    }
}
