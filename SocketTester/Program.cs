// See https://aka.ms/new-console-template for more information
using MultiSoketHub;

SocketHub hub = new SocketHub(10, "127.0.0.1", 9005);
hub.ClientConnected += OnClientConnected;
hub.MessageReceived += OnMessageRecieved;
hub.StartServer();


void OnMessageRecieved(object sender, SocketHubEventArgs e)
{
    Console.WriteLine("{Client: '" + e.Socket.RemoteEndPoint + "', Message: '" + e.Message + "'}");
}

void OnClientConnected(object sender, SocketHubEventArgs e)
{
    Console.WriteLine(e.Socket.RemoteEndPoint.ToString() + ": Connected Client");
}