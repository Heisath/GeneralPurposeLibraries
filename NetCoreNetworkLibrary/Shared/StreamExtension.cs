using System;
using System.IO;

namespace NetCoreNetwork.Shared
{
    public static class StreamExtension
    {
        public static void WriteSystemMessage(this Stream stream, SystemMessageType message)
        {
            try
            {
                stream.WriteByte((byte)message);
            }
            catch (Exception) { }
        }
        public static SystemMessageType ReadSystemMessage(this Stream stream)
        {
            try
            {
                return (SystemMessageType)(byte)stream.ReadByte();
            }
            catch (Exception) { return SystemMessageType.Nothing; }
        }

        public static void WriteInt32(this Stream stream, int v)
        {
            try
            {
                stream.Write(BitConverter.GetBytes(v), 0, 4);
            }
            catch (Exception) { }
        }
        public static int ReadInt32(this Stream stream)
        {
            try
            {
                byte[] vs = new byte[4];
                stream.Read(vs, 0, 4);
                return BitConverter.ToInt32(vs, 0);
            }
            catch (Exception) { return Int32.MinValue; }
        }

    }
}