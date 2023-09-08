using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetCoreNetwork.TCP;
using NetCoreNetwork.Shared;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml.Serialization;

namespace NetCoreNetworkDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Verbosity = Logger.Level.Everything;

            Server server = new Server(54321);

            server.OnNewConnection +=
                (Server s, Connection con) =>
                {
                    con.OnRawMessageReceived +=
                    (Connection c, byte[] data) =>
                    {
                        Console.WriteLine("* s recv: " + Encoding.UTF8.GetString(data));
                        c.SendRawMessage(Encoding.UTF8.GetBytes("Response"));
                    };
                    con.OnTextMessageReceived +=
                    (Connection c, string message) =>
                    {
                        Console.WriteLine("* sj recv: " + message);

                    };
                };

            Connection client = new Connection();
            client.Start("127.0.0.1", 54321);

            client.OnRawMessageReceived +=
                    (Connection c, byte[] data) =>
                    {
                        Console.WriteLine("* c recv: " + Encoding.UTF8.GetString(data));
                    };

            Connection client2 = new Connection();
            client2.Start("127.0.0.1", 54321);

            client2.OnRawMessageReceived +=
                    (Connection c, byte[] data) =>
                    {
                        Console.WriteLine("* c recv: " + Encoding.UTF8.GetString(data));
                    };



            while (true)
            {
                Console.ReadLine();

                client.SendRawMessage(Encoding.UTF8.GetBytes("Hallo Welt"));
                client2.SendTextMessage("Cheers");
                //  var o = new { Content = "test" };
                //client.SendJsonMessage(o);

            }



        }
    }
}
