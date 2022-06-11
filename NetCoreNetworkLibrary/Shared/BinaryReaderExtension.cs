using NetCoreNetwork.Shared;
using NetCoreNetwork.TCP;
using System.IO;
using System.Text;

namespace NetCoreNetwork.Shared
{

    public static class BinaryReaderExtension
    {
        public static string ReadLine(this BinaryReader reader)
        {
            StringBuilder result = new StringBuilder();
            char character;
            while (!reader.IsEndOfStream() && (character = reader.ReadChar()) != '\n')
                if (character != '\r' && character != '\n')
                    result.Append(character);

            return result.ToString();
        }

        public static bool IsEndOfStream(this BinaryReader reader)
        {
            if (reader.BaseStream is TcpSocketStream ns)
            {
                return !ns.IsConnected;
            }
            else
            {
                return reader.BaseStream.Position == reader.BaseStream.Length;
            }
        }
    }
}