using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.Json;

namespace NetCoreNetwork.Shared
{
    public class CryptLibrary
    {
        //The key and IV must be the same values that were used  
        //to encrypt the stream.    
        private byte[] Key = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
        private byte[] IV = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };

        private byte[] EncryptedKey;
        private byte[] EncryptedIV;

        private bool initialized;

        private byte[] PrivateRSAKey;
        private byte[] PublicRSAKey;


        public byte[] EncryptBuffer(byte[] buffer)
        {
            if (!initialized) return buffer;
            byte[] encMessage; // the encrypted bytes
            
            using (var aes = Aes.Create("AesManaged"))
            {
                aes.Key = Key;
                aes.IV = IV;
                encMessage = EncryptBytes(aes, buffer);
            }

            return encMessage;
        }
        public byte[] DecryptBuffer(byte[] buffer)
        {
            if (!initialized) return buffer;
            byte[] decMessage; // the decrypted bytes - s/b same as message

            using (var aes = Aes.Create("AesManaged"))
            {
                aes.Key = Key;
                aes.IV = IV;
                decMessage = DecryptBytes(aes, buffer);
            }

            return decMessage;
        }

        
        public void PerformServerSideKeyExchange(Stream stream)
        {
            CreateAsymmetricKey();

            byte[] size = BitConverter.GetBytes(PublicRSAKey.Length);
            stream.Write(size, 0, size.Length);
            stream.Write(PublicRSAKey, 0, PublicRSAKey.Length);
            stream.Flush();

            byte[] buf = new byte[4];
            stream.Read(buf, 0, buf.Length);

            buf = new byte[BitConverter.ToInt32(buf, 0)];
            stream.Read(buf, 0, buf.Length);

            EncryptedKey = buf;
            buf = new byte[4];
            stream.Read(buf, 0, buf.Length);

            buf = new byte[BitConverter.ToInt32(buf, 0)];
            stream.Read(buf, 0, buf.Length);

            EncryptedIV = buf;

            DecryptSymmetricKey();

            initialized = true;
        }
        public void PerformClientSideKeyExchange(Stream stream)
        {
            byte[] buf = new byte[4];
            stream.Read(buf, 0, buf.Length);

            PublicRSAKey = new byte[BitConverter.ToInt32(buf, 0)];
            stream.Read(PublicRSAKey, 0, PublicRSAKey.Length);

            CreateSymmetricKey();
            EncryptSymmetricKey();

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

        private void CreateAsymmetricKey()
        {
            //Create a new instance of the RSACryptoServiceProvider class.  
            using RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(2048);
            PublicRSAKey = RSA.ExportRSAPublicKey();
            PrivateRSAKey = RSA.ExportRSAPrivateKey();
        }

        private void CreateSymmetricKey()
        {
            var aes = Aes.Create("AesManaged");
            aes.GenerateIV();
            aes.GenerateKey();

            Key = aes.Key;
            IV = aes.IV;
        }

        private void EncryptSymmetricKey()
        {
            //Encrypt the symmetric key and IV.  
            using RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            //Import key parameters into RSA.  
            RSA.ImportRSAPublicKey(PublicRSAKey, out _);

            EncryptedKey = RSA.Encrypt(Key, false);
            EncryptedIV = RSA.Encrypt(IV, false);
        }

        private void DecryptSymmetricKey()
        {
            //Create a new instance of the RSACryptoServiceProvider class.  
            using RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            RSA.ImportRSAPrivateKey(PrivateRSAKey, out _);

            //Decrypt the symmetric key and IV.  
            Key = RSA.Decrypt(EncryptedKey, false);
            IV = RSA.Decrypt(EncryptedIV, false);
        }

        private byte[] EncryptBytes(SymmetricAlgorithm alg, byte[] message)
        {
            if ((message == null) || (message.Length == 0))
            {
                return message;
            }

            if (alg == null)
            {
                throw new ArgumentNullException("alg");
            }

            using (var stream = new MemoryStream())
            using (var encryptor = alg.CreateEncryptor())
            using (var encrypt = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
            {
                encrypt.Write(message, 0, message.Length);
                encrypt.FlushFinalBlock();
                return stream.ToArray();
            }
        }
        private byte[] DecryptBytes(SymmetricAlgorithm alg, byte[] message)
        {
            if ((message == null) || (message.Length == 0))
            {
                return message;
            }

            if (alg == null)
            {
                throw new ArgumentNullException("alg");
            }

            using (var stream = new MemoryStream())
            using (var decryptor = alg.CreateDecryptor())
            using (var encrypt = new CryptoStream(stream, decryptor, CryptoStreamMode.Write))
            {
                encrypt.Write(message, 0, message.Length);
                encrypt.FlushFinalBlock();
                return stream.ToArray();
            }
        }
    }
}
