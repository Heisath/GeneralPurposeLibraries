using System;
using System.IO;
using System.Text;

namespace NetCoreNetwork.Shared
{
    public static class BinaryWriterExtension
    {
        public static void WriteByte(this BinaryWriter writer, byte val)
        {
            try
            {
                writer.Write(val);
            }
            catch (Exception) { }
        }
        public static void WriteInt(this BinaryWriter writer, int val)
        {
            try
            {
                writer.Write(val);
            }
            catch (Exception) { }
        }
        public static void WriteLine(this BinaryWriter writer, string str)
        {
            byte[] stringbuf = Encoding.UTF8.GetBytes(str);
            byte[] lineending = new byte[] { 13, 10 };

            byte[] buf = new byte[stringbuf.Length + lineending.Length];
            stringbuf.CopyTo(buf, 0);
            lineending.CopyTo(buf, buf.Length - lineending.Length);

            try
            {
                writer.Write(buf);
            }
            catch (Exception) { }

        }

    }

}