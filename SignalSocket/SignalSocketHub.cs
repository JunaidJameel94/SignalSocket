using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading.Tasks;

namespace SignalSocket
{
    public class SignalSocketHub
    {
        private int _numberofsockets;
        private string _peerEndPoint;
        private int _port;

        public List<Socket> clients = new List<Socket>();
        public Socket serverSocket;
        public event EventHandler<SignalSocketEventArgs> MessageReceived;
        public event EventHandler<SignalSocketEventArgs> ClientConnected;
        public SignalSocketHub(int numberofsockets, string peerEndPoint,int port)
        {
            _numberofsockets = numberofsockets;
            _peerEndPoint = peerEndPoint;
            _port = port;
        }

        private byte[] privatebuffer = new byte[1024];
        public void StartServer()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(_peerEndPoint), _port));
            serverSocket.Listen(_numberofsockets);
            while (true)
            {
                try { 
                    Socket clientSocket = serverSocket.Accept();
                    
                    HandShake(clientSocket);
                    
                    clients.Add(clientSocket);
                    
                    SignalSocketEventArgs eventArgs = new SignalSocketEventArgs();
                    
                    eventArgs.Socket = clientSocket;

                    ClientConnected?.Invoke(this, eventArgs);

                    string welcomeMessage = "Connected To Server: " + clientSocket.RemoteEndPoint;
                    
                    SendToClient(clientSocket, welcomeMessage);
                    
                    StartReceiving(clientSocket);
                }
                catch (SocketException se)
                {

                }
            }
        }
        public IAsyncResult StartReceiving(Socket clientSocket)
        {
            // Begin receiving data into the 'buffer' array
           return clientSocket.BeginReceive(privatebuffer, 0, privatebuffer.Length, SocketFlags.None, ReceiveCallback, clientSocket);
        }
        public void ReceiveCallback(IAsyncResult ar)
        {
            Socket clientSocket = (Socket)ar.AsyncState;
            
            try
            {
                int bytesRead = clientSocket.EndReceive(ar);

                if (bytesRead > 0)
                {
                    byte[] receivedData = new byte[bytesRead];
                    Array.Copy(privatebuffer, 0, receivedData, 0, bytesRead);
                    byte opcode = receivedData[0];
                    byte payloadLength = (byte)(receivedData[1] & 0x7F);
                    bool isMasked = (receivedData[1] & 0x80) != 0;

                    if (isMasked)
                    {
                        byte[] maskingKey = new byte[] { receivedData[2], receivedData[3], receivedData[4], receivedData[5] };
                        byte[] payload = new byte[payloadLength];
                        for (int i = 0; i < payloadLength; i++)
                        {
                            payload[i] = (byte)(receivedData[i + 6] ^ maskingKey[i % 4]);
                        }
                        string message = Encoding.UTF8.GetString(payload);
                        SignalSocketEventArgs receivedEventArgs = new SignalSocketEventArgs();
                        receivedEventArgs.Message = message;
                        receivedEventArgs.Socket = clientSocket;
                        MessageReceived?.Invoke(this, receivedEventArgs);
                    }
                    StartReceiving(clientSocket);
                }
                else
                {
                    clients.Remove(clientSocket);
                    clientSocket.Close();
                }
            }
            catch (SocketException)
            {
                clients.Remove(clientSocket);
                clientSocket.Close();
            }
            catch (Exception ex)
            {
                clients.Remove(clientSocket);
                clientSocket.Close();
            }
        }
        public void BroadcastToClients(string message, Socket sender)
        {
            foreach (Socket client in clients)
            {
                if (client != sender)
                {
                    SendToClient(client, message);
                }
            }
        }
        public void SendToClient(Socket client, string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] frame = new byte[messageBytes.Length + 2];
            frame[0] = 0x81; // FIN bit set to 1, Text frame
            frame[1] = (byte)messageBytes.Length; // Payload length
            Array.Copy(messageBytes, 0, frame, 2, messageBytes.Length);
            client.Send(frame);
        }
        public void HandShake(Socket clientSocket)
        {
            byte[] buffer = new byte[1024];
            clientSocket.Receive(buffer);

            
            string websocket_header = Encoding.UTF8.GetString(buffer);

            string matchs = new System.Text.RegularExpressions.Regex("Sec-WebSocket-Key: (.*)").Match(websocket_header).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string finalAcceptedKey = Convert.ToBase64String(System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(matchs)));

            // Write the WebSocket handshake response
            string responseHeader = "";
            responseHeader += "HTTP/1.1 101 Switching Protocols\r\n";
            responseHeader += "Connection: Upgrade\r\n";
            responseHeader += "Upgrade: websocket\r\n";
            responseHeader += "Sec-WebSocket-Accept: " + finalAcceptedKey + "\r\n";
            responseHeader += "\r\n";
            byte[] sendResponse = Encoding.UTF8.GetBytes(responseHeader);
            clientSocket.Send(sendResponse);

        }

    }
    public class SignalSocketEventArgs:EventArgs
    {
        public string Message { get; set; }
        public Socket Socket { get; set; }
    }
}
