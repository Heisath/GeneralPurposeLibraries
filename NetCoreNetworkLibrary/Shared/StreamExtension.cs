using System;
using System.IO;

namespace NetCoreNetworkLibrary.Shared
{
    public static class StreamExtension
    {
        public static void WriteSystemMessage(this Stream stream, SystemMessageType message)
        {
            stream.WriteByte((byte)message);
        }
        public static SystemMessageType ReadSystemMessage(this Stream stream)
        {
            return (SystemMessageType)(byte)stream.ReadByte();
        }

        public static void WriteInt32(this Stream stream, int v)
        {
            stream.Write(BitConverter.GetBytes(v), 0, 4);
        }
        public static int ReadInt32(this Stream stream)
        {
            byte[] vs = new byte[4];
            stream.Read(vs, 0, 4);
            return BitConverter.ToInt32(vs, 0);
        }

    }
}