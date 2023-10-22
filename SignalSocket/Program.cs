using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Security.Cryptography;
namespace SignalSocket
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SignalSocketHub hub = new SignalSocketHub(10,"127.0.0.1",9005);
            hub.ClientConnected += OnClientConnected;
            hub.MessageReceived += OnMessageRecieved;
            hub.StartServer();
        }
        public static void OnMessageRecieved(object sender, SignalSocketEventArgs e)
        {
            Console.WriteLine("{Client: '" + e.Socket.RemoteEndPoint  + "', Message: '" + e.Message + "'}");
        }

        public static void OnClientConnected(object sender, SignalSocketEventArgs e)
        {
            Console.WriteLine(e.Socket.RemoteEndPoint.ToString() + ": Connected Client");
        }
    }
}
