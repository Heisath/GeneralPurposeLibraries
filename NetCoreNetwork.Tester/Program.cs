using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetCoreNetworkLibrary.TCP;
using NetCoreNetworkLibrary.Shared;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml.Serialization;

namespace NetCoreNetworkLibrary.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Verbosity = Logger.Level.Everything;

            CryptLibrary.PrepareRSA();
            Server server = new Server(12345);

            server.OnNewConnection +=
                (Server s, Connection con) =>
                {
                    con.OnRawMessageReceived +=
                    (Connection c, byte[] data) =>
                    {
                        Console.WriteLine("* s recv: " + Encoding.UTF8.GetString(data));
                        c.SendRawMessage(Encoding.UTF8.GetBytes("Response"));
                    };
                    con.OnJsonMessageReceived +=
                    (Connection c, dynamic jsonObject) =>
                    {
                        Console.WriteLine("* sj recv: " + jsonObject.Content);

                    };
                };

            Connection client = new Connection();
            client.Start("127.0.0.1", 12345);

            client.OnRawMessageReceived +=
                    (Connection c, byte[] data) =>
                    {
                        Console.WriteLine("* c recv: " + Encoding.UTF8.GetString(data));
                    };

            Connection client2 = new Connection();
            client2.Start("127.0.0.1", 12345);

            client2.OnRawMessageReceived +=
                    (Connection c, byte[] data) =>
                    {
                        Console.WriteLine("* c recv: " + Encoding.UTF8.GetString(data));
                    };



            while (true)
            {
                Console.ReadLine();

                client.SendRawMessage(Encoding.UTF8.GetBytes("Hallo Welt"));
                client2.SendJsonMessage(new { Content = "Cheers" });
                //  var o = new { Content = "test" };
                //client.SendJsonMessage(o);

            }



        }
    }
}
