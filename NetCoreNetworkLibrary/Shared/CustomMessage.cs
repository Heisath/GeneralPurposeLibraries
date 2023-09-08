using NetCoreNetwork.TCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetCoreNetwork.Shared
{
    public class CustomMessage : IDisposable
    {
        public Connection? Connection { get; private set; }

        private MemoryStream _data;
        public BinaryReader Reader { get; private set; }
        public BinaryWriter Writer { get; private set; }

        public CustomMessage(Connection? connection = null) {
            _data = new MemoryStream();
            Reader = new BinaryReader(_data);
            Writer = new BinaryWriter(_data);
            Connection = connection;
        }
        public CustomMessage(byte[] messageBytes, Connection? connection = null)
        {
            _data = new MemoryStream(messageBytes);
            Reader = new BinaryReader(_data);
            Writer = new BinaryWriter(_data);
            Connection = connection;
        }

        public void Send(Connection connection)
        {
            connection.SendRawMessage(_data.ToArray());
        }
        public void Send()
        {
            Connection?.SendRawMessage(_data.ToArray());
        }


        public void Dispose()
        {
            Reader.Dispose();
            Writer.Dispose();
            _data.Dispose();
        }
    }
}
