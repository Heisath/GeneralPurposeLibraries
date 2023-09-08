using NetCoreNetwork.Shared;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetCoreNetwork.TCP
{
    public class Connection
    {
        public delegate void JsonMessageReceivedHandler(Connection sender, dynamic message);
        public event JsonMessageReceivedHandler? OnJsonMessageReceived;

        public delegate void RawMessageReceivedHandler(Connection sender, byte[] message);
        public event RawMessageReceivedHandler? OnRawMessageReceived;

        public delegate void ConnectionInfoHandler(Connection sender);
        public event ConnectionInfoHandler? OnConnected;
        public event ConnectionInfoHandler? OnDisconnected;
        public event ConnectionInfoHandler? OnConnectionLost;

        public ConnectionSettings Settings { get; set; } = new ConnectionSettings();


        private Socket? socket;
        private TcpSocketStream? stream;
        public IPEndPoint? EndPoint { get; private set; }
        public bool Connected { get; private set; }
        public bool IsClient { get; private set; }

        private DateTime lastAttempt;

        public TimeSpan RoundTripTime { get; private set; }
        private DateTime lastPingSent;
        private DateTime lastPingRecv;
        private byte pingMsg;

        private Thread? thread;
        private bool threadRunning;

        private CryptLibrary? Encryption { get; set; }

        public Connection()
        {
            this.socket = null;
            this.stream = null;
            this.EndPoint = null;
        }

        public void Start(string host, int port)
        {
            Start(IPEndPointFactory.Parse(host, port));
        }
        public void Start(string endpointstring)
        {
            Start(IPEndPointFactory.Parse(endpointstring));
        }
        public void Start(IPEndPoint remote)
        {
            EndPoint = remote;
            IsClient = true;

            StartThread();
            Logger.WriteLine("Client started.", Logger.Level.Info);
        }

        internal static Connection? ProcessConnectionRequest(Socket newSocket)
        {
            TcpSocketStream newStream = new TcpSocketStream(newSocket, true);

            using (BinaryReader newReader = new BinaryReader(newStream, Encoding.UTF8, true))
            {
                string firstLine = newReader.ReadLine();

                if (firstLine != "HELO")
                {
                    return null;
                }
            }

            Logger.WriteLine("Received HELO, starting encryption", Logger.Level.Info);
            
            CryptLibrary newEncryption = new CryptLibrary();
            newEncryption.PerformServerSideKeyExchange(newStream);

            Logger.WriteLine("Connection is encrypted", Logger.Level.Info);

            Connection connection = new Connection
            {
                socket = newSocket,
                stream = newStream,
                Encryption = newEncryption,
                IsClient = false,
                EndPoint = newSocket.RemoteEndPoint as IPEndPoint,
                lastPingSent = DateTime.Now,
                lastPingRecv = DateTime.Now
            };

            connection.StartThread();
            return connection;
        }

        private void StartThread()
        {
            threadRunning = true;
            thread = new Thread(
                () =>
                {
                    try
                    {
                        while (Thread.CurrentThread.ThreadState == ThreadState.Running && threadRunning)
                        {
                            Update();
                            Thread.Sleep(1);
                        }
                    }
                    catch (ThreadAbortException)
                    {

                    }
                });
            thread.Start();
        }

        private void Connect()
        {
            if (EndPoint == null)
                throw new NullReferenceException("EndPoint not selected!");

            socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { Blocking = true, NoDelay = true };
            try
            {
                socket.Connect(EndPoint);
            }
            catch (Exception) { }
            if (!socket.Connected)
            {
                return;
            }

            Logger.WriteLine("Connect success, sending HELO", Logger.Level.Info);

            stream = new TcpSocketStream(socket, true);
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.WriteLine("HELO");
                writer.Flush();
            }

            Logger.WriteLine("Enabling encryption", Logger.Level.Info);

            Encryption = new CryptLibrary();
            Encryption.PerformClientSideKeyExchange(stream);

            Logger.WriteLine("Connection is encrypted!", Logger.Level.Info);

            stream.WriteSystemMessage(SystemMessageType.Connect);

            lastPingSent = DateTime.Now;
            lastPingRecv = DateTime.Now;
        }

        private void Update()
        {
            if (IsClient && !Connected)
            {
                if (DateTime.Now - lastAttempt > TimeSpan.FromSeconds(Settings.ConnectInterval))
                {
                    Logger.WriteLine("Connecting..", Logger.Level.Info);
                    Connect();
                    lastAttempt = DateTime.Now;
                }
            }

            if (Connected)
            {
                if (DateTime.Now - lastPingSent > TimeSpan.FromSeconds(Settings.PingInterval))
                {
                    SendPing();
                }

                if (DateTime.Now - lastPingRecv > TimeSpan.FromSeconds(Settings.PingTimeout))
                {
                    Stop();
                    OnConnectionLost?.Invoke(this);
                    return;
                }
            }

            while ((stream?.IsConnected ?? false) && (stream?.DataAvailable ?? false))
            {
                SystemMessageType cmd = stream.ReadSystemMessage();
                switch (cmd)
                {
                    case SystemMessageType.Ping:
                        Logger.WriteLine("Got ping message, responding", Logger.Level.Info);
                        byte pingRet = (byte)stream.ReadByte();
                        stream.WriteSystemMessage(SystemMessageType.PingOk);
                        stream.WriteByte(pingRet);
                        lastPingRecv = DateTime.Now;
                        break;
                    case SystemMessageType.PingOk:
                        byte by = (byte)stream.ReadByte();
                        if (by == pingMsg)
                        {
                            RoundTripTime = DateTime.Now - lastPingSent;
                        }
                        Logger.WriteLine("Received ping, rtt: " + RoundTripTime.TotalMilliseconds, Logger.Level.Info);
                        break;
                    case SystemMessageType.Connect:
                        Logger.WriteLine("Connection request, ok", Logger.Level.Info);
                        Connected = true;
                        stream.WriteSystemMessage(SystemMessageType.ConnectOk);
                        OnConnected?.Invoke(this);
                        break;
                    case SystemMessageType.ConnectOk:
                        Connected = true;
                        OnConnected?.Invoke(this);
                        Logger.WriteLine("Connection accepted", Logger.Level.Info);
                        break;

                    case SystemMessageType.Disconnect:
                        Logger.WriteLine("Disconnect recv.", Logger.Level.Info);
                        Stop(keepAlive: false);
                        OnDisconnected?.Invoke(this);
                        return;
                    case SystemMessageType.JsonMessage:
                        {
                            int length = stream.ReadInt32();
                            byte[] encryptedData = new byte[length];
                            stream.Read(encryptedData, 0, length);

                            byte[]? decryptedData = Encryption?.DecryptBuffer(encryptedData);
                            if (decryptedData != null)
                            {
                                string jsonString = Encoding.UTF8.GetString(decryptedData);

                                //dynamic jsonObject = System.Text.Json.JsonSerializer.DeserializeObject(jsonString);

                                //OnJsonMessageReceived(this, jsonObject);
                            }
                        }
                        break;
                    case SystemMessageType.RawMessage:
                        {

                            int length = stream.ReadInt32();
                            byte[] encryptedData = new byte[length];
                            stream.Read(encryptedData, 0, length);

                            byte[]? decryptedData = Encryption?.DecryptBuffer(encryptedData);
                            if (decryptedData != null)
                                OnRawMessageReceived?.Invoke(this, decryptedData);
                        }
                        break;
                    default:
                        Logger.WriteLine("Unknown command > " + cmd, Logger.Level.Warning);// + " " + command);
                        break;
                }
            }
        }

        public bool SendJsonMessage(object jsonObject)
        {
            if (stream == null || Encryption == null)
                return false;

            string jsonString = System.Text.Json.JsonSerializer.Serialize(jsonObject);
            byte[] decryptedData = Encoding.UTF8.GetBytes(jsonString);
            byte[] encryptedData = Encryption.EncryptBuffer(decryptedData);

            try
            {
                stream.WriteSystemMessage(SystemMessageType.JsonMessage);
                stream.WriteInt32(encryptedData.Length);
                stream.Write(encryptedData, 0, encryptedData.Length);
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public bool SendRawMessage(byte[] decryptedData)
        {
            if (stream == null || Encryption == null)
                return false;

            byte[] encryptedData = Encryption.EncryptBuffer(decryptedData);

            try
            {
                stream.WriteSystemMessage(SystemMessageType.RawMessage);
                stream.WriteInt32(encryptedData.Length);
                stream.Write(encryptedData, 0, encryptedData.Length);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SendPing()
        {
            if (stream == null) return false;
            try
            {
                pingMsg = (byte)((new Random()).Next(1, 254));
                stream.WriteSystemMessage(SystemMessageType.Ping);
                stream.WriteByte(pingMsg);
                lastPingSent = DateTime.Now;
                Logger.WriteLine("Sent ping.", Logger.Level.Info);
                return true;
            }
            catch(IOException exception)
            {
                Logger.WriteLine($"Ping failed: {exception.Message}", Logger.Level.Error);
                return false;
            }
        }

        public void Stop()
        {
            this.Stop(false);
        }
        private void Stop(bool? keepAlive = null)
        {
            Logger.WriteLine("closing connection.", Logger.Level.Info);

            if (keepAlive == null)
            {
                keepAlive = IsClient;
            }

            if (keepAlive == false) threadRunning = false;
            if (!Connected) return;

            stream?.WriteSystemMessage(SystemMessageType.Disconnect);

            Connected = false;
            RoundTripTime = TimeSpan.Zero;

            stream?.Close();
            socket?.Close();

        }

        public void Restart() => Stop(true);


    }
}
