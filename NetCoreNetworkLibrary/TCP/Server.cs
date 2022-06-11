using NetCoreNetwork.Shared;
using System;
using System.Net;
using System.Net.Sockets;

namespace NetCoreNetwork.TCP
{
    public class Server : IDisposable
    {
        public delegate void NewConnectionHandler(Server sender, Connection client);
        public event NewConnectionHandler OnNewConnection;

        private Socket serverSocket;
        public int Port { get; private set; }

        public Server(int port)
        {
            serverSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            Port = port;

            serverSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
            serverSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, Port));
            serverSocket.NoDelay = true;
            serverSocket.Blocking = true;

            serverSocket.Listen(5);

            serverSocket.BeginAccept(AcceptCallback, null);

            Logger.WriteLine("Server started, Port " + port, Logger.Level.Info);
        }

        private void AcceptCallback(IAsyncResult result)
        {
            try
            {
                Socket newSocket = serverSocket.EndAccept(result);
                if (newSocket != null)
                {
                    Connection newConnection = Connection.ProcessConnectionRequest(newSocket);
                    if (newConnection != null) OnNewConnection?.Invoke(this, newConnection);
                }
            }
            catch (Exception e)
            {
                Logger.WriteLine("Exception: " + e.Message, Logger.Level.Error);
            }
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        public void Dispose()
        {
            serverSocket.Close();
            serverSocket = null;
        }

    }
}
