using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace GeneralPurposeNetworkLib.Shared
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

        private static RSAParameters privateRSAParams;
        private static RSAParameters publicRSAParams;

        public byte[] EncryptBuffer(byte[] buffer)
        {
            if (!initialized) return buffer;
            byte[] encMessage; // the encrypted bytes
            
            using (var rijndael = new RijndaelManaged())
            {
                rijndael.Key = Key;
                rijndael.IV = IV;
                encMessage = EncryptBytes(rijndael, buffer);
            }

            return encMessage;
        }
        public byte[] DecryptBuffer(byte[] buffer)
        {
            if (!initialized) return buffer;
            byte[] decMessage; // the decrypted bytes - s/b same as message

            using (var rijndael = new RijndaelManaged())
            {
                rijndael.Key = Key;
                rijndael.IV = IV;
                decMessage = DecryptBytes(rijndael, buffer);
            }

            return decMessage;
        }

        public static void PrepareRSA()
        {
            //Create a new instance of the RSACryptoServiceProvider class.  
            using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(2048))
            {
                publicRSAParams = RSA.ExportParameters(false);
                privateRSAParams = RSA.ExportParameters(true);
            }
        }
        public void PerformServerSideKeyExchange(Stream stream)
        {
            byte[] buffer;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, publicRSAParams);

                buffer = ms.ToArray();
            }

            byte[] size = BitConverter.GetBytes(buffer.Length);
            stream.Write(size, 0, size.Length);
            stream.Write(buffer, 0, buffer.Length);
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

            DecryptRSA();

            initialized = true;
        }
        public void PerformClientSideKeyExchange(Stream stream)
        {
            byte[] buf = new byte[4];
            stream.Read(buf, 0, buf.Length);

            buf = new byte[BitConverter.ToInt32(buf, 0)];
            stream.Read(buf, 0, buf.Length);

            RSAParameters rsaparams;
            using (MemoryStream ms = new MemoryStream(buf))
            {
                BinaryFormatter bf = new BinaryFormatter();
                object obj = bf.Deserialize(ms);

                if (obj is RSAParameters)
                {
                    rsaparams = (RSAParameters)obj;
                    publicRSAParams = rsaparams;
                    CreateSymmetricKey();
                    EncryptRSA();
                }
            }
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

        private void CreateSymmetricKey()
        {
            //Create a new instance of the RijndaelManaged class.  
            RijndaelManaged RM = new RijndaelManaged();
            RM.GenerateIV();
            RM.GenerateKey();

            Key = RM.Key;
            IV = RM.IV;
        }
        private void EncryptRSA()
        {
            //Encrypt the symmetric key and IV.  
            using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
            {
                //Import key parameters into RSA.  
                RSA.ImportParameters(publicRSAParams);

                EncryptedKey = RSA.Encrypt(Key, false);
                EncryptedIV = RSA.Encrypt(IV, false);
            }
        }
        private void DecryptRSA()
        {
            //Create a new instance of the RSACryptoServiceProvider class.  
            using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
            {
                RSA.ImportParameters(privateRSAParams);

                //Decrypt the symmetric key and IV.  
                Key = RSA.Decrypt(EncryptedKey, false);
                IV = RSA.Decrypt(EncryptedIV, false);
            }
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
